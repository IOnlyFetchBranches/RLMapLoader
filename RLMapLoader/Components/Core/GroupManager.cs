using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Cloud.PubSub.V1;
using RLMapLoader.Components.Core.Constants;
using RLMapLoader.Components.Helpers.Extensions;
using RLMapLoader.Components.Models;

namespace RLMapLoader.Components.Core
{
    public class GroupManager : PubSubComponent
    {
        
        private UserModule _user;
       
        private FirestoreDb _db;
        public bool IsInitialized => _db != null && _user != null;

        public GroupManager(UserModule withUser)
        {
            _user = withUser;
           
        }
        public async Task<bool> InitializeAsync(FirestoreDb injectedDb = null)
        {
            try
            {
                _db = injectedDb ?? await FirestoreDb.CreateAsync(GlobalConstants.G_PROJ_NAME);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not create publisher client!", e);
            }
            return true;
        }

        /// <summary>
        /// Checks, Validates User groups, then returns the valid groups.
        /// </summary>
        /// <returns>Valid group list</returns>
        public async Task<Dictionary<string, string>> CheckUserGroupsAsync()
        {
            var retDict = new Dictionary<string, string>();
            if (!_user.UserModel.IsInGroup)
            {
                return null;
            }
            foreach (var groupId in _user.UserModel.MemberGroups)
            {
                if (groupId.Length < 0)
                {
                    _logger.LogDebug($"Found empty group ID, check tables for user! id:{_user.UserModel.ID}");
                    continue;
                }
                var groupRef = await _db.Collection("groups").Document(groupId).GetSnapshotAsync();
                if (!groupRef.Exists)
                {
                    _logger.LogWarning("User registered to invalid group. Cleaning...");
                    await CleanDanglingGroupFromUserAsync(groupId);
                    continue;
                }
                retDict.Add(groupId, groupRef.GetValue<string>("Name"));
            }

            return retDict;
        }

        private async Task CleanDanglingGroupFromUserAsync(string groupId)
        {
            var userSnap = await _db.Collection("users").Document(_user.UserModel.ID).GetSnapshotAsync();
            var user = userSnap.ToUserModel();
            user.MemberGroups.Remove(groupId);
            if (user.OwnedGroups.Contains(groupId)) user.OwnedGroups.Remove(groupId);
            if (user.CreatedGroups.Contains(groupId)) user.CreatedGroups.Remove(groupId);
            _logger.LogDebug("Cleaning user...");
            await (userSnap.Reference.SetAsync(user, SetOptions.Overwrite));
            _user.UserModel = user; //set app user

        }

        public async Task<bool> LeaveGroupAsync(string groupId)
        {
          
            var user = _user.UserModel;
          
            if (!user.MemberGroups.Contains(groupId))
            {
                _logger.LogError("You are not in group " + groupId);
                var userGroups = await CheckUserGroupsAsync();
                Console.WriteLine("You are currently in:\n" + userGroups?.ToKeyPairString());
                return false;
            }

            
            //load group
            var groupRes = _db.Collection("groups").Document(groupId);
            var snap = await groupRes.GetSnapshotAsync();
            var group = new Group
            {
                ID = snap.Id,
                Members = snap.GetValue<List<string>>("Members"),
                Name = snap.GetValue<string>("Name"),
                IsNew = snap.GetValue<bool>("IsNew"),
                Owner = snap.GetValue<string>("Owner"),
            };

            var groupDocument = _db.Collection("groups").Document(groupId);
            //if owner
            if (group.Owner == user.ID)
            {
                if (group.Members.Count == 1)
                {
                    //just delete group.
                    await groupDocument.DeleteAsync();
                    //Remove stream
                    await DeleteTopicAsync(groupId);
                    //TODO: possibly consolidate user update logic BACK to userModule itself instead of doin here.
                    user.MemberGroups.Remove(groupId);
                    user.OwnedGroups.Remove(groupId);
                    if(user.CreatedGroups.Contains(groupId)) user.CreatedGroups.Remove(groupId);
                    user.IsInGroup = user.MemberGroups.Any();
                    await _user.UserReference.SetAsync(user, SetOptions.MergeAll);
                    return true;

                }


                var promptRes = PromptForNewOwner(group);
                if (promptRes == groupId)
                {
                    await groupDocument.DeleteAsync();
                    await DeleteTopicAsync(groupId);
                    //TODO: possibly consolidate user update logic BACK to userModule itself instead of doin here.
                    user.MemberGroups.Remove(groupId);
                    user.OwnedGroups.Remove(groupId);
                    if (user.CreatedGroups.Contains(groupId)) user.CreatedGroups.Remove(groupId);
                    user.IsInGroup = user.MemberGroups.Any();
                    await _user.UserReference.SetAsync(user, SetOptions.MergeAll);
                    return true;
                }
                else if (promptRes == "cancel") return false;
                else
                {
                    _logger.LogInfo("Changing owner");
                    //change owner
                    group.Owner = promptRes;
                    //remove self from group
                    group.Members.Remove(user.ID);
                    await groupDocument.SetAsync(group, SetOptions.MergeFields("Members"));
                    //TODO: possibly consolidate user update logic BACK to userModule itself instead of doin here.
                    user.MemberGroups.Remove(groupId);
                    user.OwnedGroups.Remove(groupId);
                    if (user.CreatedGroups.Contains(groupId)) user.CreatedGroups.Remove(groupId);
                    await RelateNewOwnerAsync(groupId, promptRes);
                    user.IsInGroup = user.MemberGroups.Any();
                    //sync user
                    await _user.UserReference.SetAsync(user, SetOptions.MergeAll);
                }
               
                return true;
            }
            else
            {
                //update group to remove user and user to remove group (not the owner)
                group.Members.Remove(user.ID);
                await groupDocument.SetAsync(group, SetOptions.MergeFields("Members"));
                //TODO: possibly consolidate user update logic BACK to userModule itself instead of doin here.
                user.MemberGroups.Remove(groupId);
                user.IsInGroup = user.MemberGroups.Any();
                await _user.UserReference.SetAsync(user, SetOptions.MergeAll);
                _user.UserModel = user; //update user state

                
                return true;
            }


            
           
        }

        private async Task<bool> RelateNewOwnerAsync(string groupId, string newOwner )
        {
            //Does nothing right now, because i don't necessarily care if a user gets assigned owner to ALL the groups that they own. This really should be 'CreatedGroups'
            var userSnap = await _db.Collection("users")
                .Document(newOwner).GetSnapshotAsync();
            if (!userSnap.Exists)
            {
                throw new Exception("Count not find user for assignment!");
            }

            var user = userSnap.ToUserModel();

            user.OwnedGroups.Add(groupId);
            await userSnap.Reference.SetAsync(user, SetOptions.MergeFields("OwnedGroups"));
            return true;
        }

        private async Task DeleteTopicAsync(string groupId)
        {
            var topicName = new TopicName(GlobalConstants.G_PROJ_NAME, groupId);
            await PubService.DeleteTopicAsync(topicName);
        }

        private string PromptForNewOwner(Group group)
        {
            Console.WriteLine($@"You are the owner of this group, you'll need to pick another person OR type the group-id {group.ID} to delete group. You may cancel by typing 'cancel'.
Members:{GetGroupMembers(group)}");
            string res = "";
            while (!NewOwnerResponseValid(group, res))
            {
                res = Console.In.ReadLine();
            }

            return res;
        }

        private bool NewOwnerResponseValid(Group group, string res)
        {
            return group.Members.Contains(res) || res == group.ID || res == "cancel";
        }

        public async Task<bool> JoinGroupAsync(string groupId)
        {
            if (!IsInitialized)
            {
                throw new Exception("Component not initialized!");
            }
            var user = _user.UserModel;

            if (user.MemberGroups.Contains(groupId))
            {
                _logger.LogError("You are already in that group. Lol. Call 'sync group list'.");
                return false;
            }
           
            var groupDoc = _db.Collection("groups").Document(groupId);
            var groupSnap = await groupDoc.GetSnapshotAsync();
            if (!groupSnap.Exists)
            {
                _logger.LogError("Did not find group of id " + groupId);
                return false;
            }
            _logger.LogInfo("Adding user to group...");
            //update group with new user
           
            var membersList = groupSnap.GetValue<List<string>>("Members");
            if (!membersList.Contains(user.ID))
            {
                membersList.Add(user.ID);
            }
            await groupDoc.SetAsync(
                new Group {Members = membersList}, SetOptions.MergeFields("Members"));

            user.IsInGroup = true;

            //register group to user
            if (!user.MemberGroups.Contains(groupId))
            {
                user.MemberGroups.Add(groupId);
                _logger.LogInfo("Finishing registration...");
                await _user.UserReference.SetAsync(user, SetOptions.MergeAll);
            }
            _user.UserModel = user;

            _logger.LogInfo("Done.");
            return true;
        }

        //This creates the group object + the stream.
        public async Task<bool> CreateGroupAsync(string groupName)
        {
            if (!IsInitialized)
            {
                throw new Exception("Component not initialized!");
            }
            if (CheckHasGroupsLeft(_user))
            {
                (bool errorPresent, Group groupDbResponse) groupRes= (false,null);
                try
                {
                    _logger.LogDebug("Registering group info...");
                    groupRes = await CreateAndRegisterFbseGroupAsync(groupName);
                    _logger.LogDebug("Registering group stream info...");
                    await CreateGroupTopicAsync(groupRes.groupDbResponse.ID);
                    _logger.LogInfo("Done.");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to create group!", e);
                   
                    if (groupRes.groupDbResponse != null)
                    {
                        _logger.LogInfo("Attempting cleanup of group artifacts...");
                        try
                        {
                            await LeaveGroupAsync(groupRes.groupDbResponse.ID);
                        }
                        catch (Exception ie)
                        {
                            var groupId = groupRes.groupDbResponse.ID;
                            _logger.LogError("May not have cleaned up completely, resetting state so you can recall create. Then remove the bad group, if it still shows in your list. " + groupId);
                           
                        }
                        
                    }
                }
            }
            else
            {
                _logger.LogError("Must delete groups before creating additional ones!");
                return false;
            }

            return true;
        }

        private  bool CheckHasGroupsLeft(UserModule user)
        {
            if (!IsInitialized)
            {
                throw new Exception("Component not initialized!");
            }

            return _user.UserModel.CreatedGroups.Count < _user.UserModel.GroupLimit;
        }

        private async Task CreateGroupTopicAsync(string groupId)
        {
            if (!IsInitialized)
            {
                throw new Exception("Component not initialized!");
            }

            var topicName = new TopicName(GlobalConstants.G_PROJ_NAME, groupId);
            _logger.LogInfo("Creating group topic...");
            await PubService.CreateTopicAsync(topicName);
        }

        private  string GetGroupMembers(Group forGroup)
        {
            var sb = new StringBuilder();
            forGroup.Members.ForEach(async (memberId) =>
            {
                sb.AppendLine($"{memberId}");
            });
            return sb.ToString();

        }
        private async Task<(bool errorPresent, Group groupDbResponse)> CreateAndRegisterFbseGroupAsync(string groupName)
        {
            
                var groupToInsert = new Group
                {
                    Name = groupName,
                    Members = new List<string> {_user.UserModel.ID},
                    IsNew = true,
                    Owner = _user.UserModel.ID,
                };


                _logger.LogInfo("Sending group information to server...");
                var res = await _db.Collection("groups").AddAsync(groupToInsert);
                
                var user = _user.UserModel;
                user.IsInGroup = true;
                //register group to owner
                user.MemberGroups.Add(res.Id);
                user.OwnedGroups.Add(res.Id);
                user.CreatedGroups.Add(res.Id);

                _logger.LogInfo("Finishing group registration with server...");
                //Merge new group additions
                var userRes = await _db.Collection("users").Document(user.ID).SetAsync(user, SetOptions.MergeAll);
                _user.UserModel =  user; //update user 

                groupToInsert.ID = res.Id;
                return (true, groupToInsert);
            }
           
        }
}
