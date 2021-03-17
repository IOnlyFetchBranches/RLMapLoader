using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RLMapLoader.Components.Helpers.Extensions;
using RLMapLoader.Components.Models;


namespace RLMapLoader.Components.Core
{
    public sealed class SyncModule : PubSubComponent
    {
        private const string GROUP_FEED_NAME = "groupactivity";
        private IConfigurationRoot _configuration;
        private string _uid;
        private UserModule _user;
        private GroupManager _groupManager;
        private MapLoaderState _state;

        public SyncModule(ref MapLoaderState withState, ref UserModule withUser)
        {
            _configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(Program).Assembly)
                .Build();

            _groupManager = new GroupManager(withUser);
            _user = withUser;
            _state = withState;
        }


        //TODO: convert this to async, once GUI rolls out.
        public int Run(string[] args)
        {
            if (!CheckArgs(args, 2))
            {
                _logger.LogError("Invalid arguments, format is 'sync <syncCommand>' see 'sync help' for more info");
                return 1;
            }

            try
            {
                var syncCommand = args[1];
                switch (syncCommand)
                {
                    case "group":
                        //TODO:if any error occurs during this, we need to set the appropriate flag so we can respond at a later time
                        //TODO: setup a job that can catch any orphaned groupIds in even of failure mid way
                        return RunGroupCommandsAsync(args).Result;
                    case "status":
                        return 1; //Doesnt do anything yet
                    case "on":
                        //starts a sync session with the group
                        throw new NotImplementedException();
                    case "help":
                        ShowHelp();
                        return 0;
                    default:
                        ShowHelp();
                        return 1;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Encountered error processing sync module command {args[1]}.", e);
                return 1;
            }
        }

        

        //TODO: implmenet group manager commands fully;
        private async Task<int> RunGroupCommandsAsync(string[] args)
        {
            if (!CheckArgs(args,3))
            {
                _logger.LogError("Invalid argument count. sync group <groupCommand> <groupArguments> is format. See 'sync group help' for more info");
                return 1;
            }

            if (!_groupManager.IsInitialized)
            {
                await _groupManager.InitializeAsync();
            }

            var groupCommand = args[2];

            switch (groupCommand)
            {
                case "list":
                    await ListGroupsAsync();
                    return 0;

                case "create":
                    if (!CheckArgs(args, 4, true))
                    {
                        _logger.LogError("Invalid argument count. sync group create <groupName> is format.");
                        return 1;
                    }
                    var groupName = args[3];
                    _logger.LogInfo("Begin group creation...");
                    var isGroupCreated = await _groupManager.CreateGroupAsync(groupName);

                    if (!isGroupCreated)
                    {
                        return 1;
                    }

                    return 0;
                case "join":
                    if (!CheckArgs(args,4, true))
                    {
                        _logger.LogError("Invalid argument count. sync group join <id> is format.");
                        return 1;
                    }
                    var groupId = args[3];
                    var ok =  await _groupManager.JoinGroupAsync(groupId);

                    if (!ok)
                    {
                        return 1;
                    }

                    return 0;

                case "leave":
                    if (!CheckArgs(args, 4, true))
                    {
                        _logger.LogError("Invalid argument count. sync group leave <id> is format.");
                        return 1;
                    }
                    var groupId2 = args[3];
                    _logger.LogInfo($"Leaving group...");
                    var wasGroupLeaveSucessful = await _groupManager.LeaveGroupAsync(groupId2);

                    if (!wasGroupLeaveSucessful)
                    {
                        return 1;
                    }
                    else
                    {
                        _logger.LogInfo($"Done.");
                    }

                    return 0;
                case "help":
                    ShowGroupHelp();
                    return 0;

                default:
                    ShowGroupHelp();
                    return 1;
                    
            }
        }

        private async Task ListGroupsAsync()
        {
            var groups = await _groupManager.CheckUserGroupsAsync();
            if (groups == null)
            {
                Console.WriteLine($@"Did not find any groups for logged in user {_user.UserModel.ID}. Lol.");
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Current groups for logged in user {_user.UserModel.ID}: ");
            sb.AppendLine();
            sb.AppendLine(groups.ToKeyPairString());

            Console.WriteLine(sb.ToString());
        }

        private void ShowHelp()
        {
            Console.WriteLine(@"Sync module currently supports the following commands: group, on <groupId>, off, help");
        }
        private void ShowGroupHelp()
        {
            Console.WriteLine(
                @"Group Manager currently supports the following commands: create <groupName>, join <groupId>, leave <groupId>, list, help"
            );

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expectedLength"></param>
        /// <param name="exactLength"></param>
        /// <param name="lastArgCanBeNull">Does not matter for length 1 argument arrays!</param>
        /// <returns></returns>
        private bool CheckArgs(string[] args, int expectedLength, bool exactLength = false, bool lastArgCanBeNull = false)
        {
            if (args.Length == 1)
            {
                return args.Length == expectedLength;
            }
            if (lastArgCanBeNull && exactLength)
            {
                return (args.Length == expectedLength);
            }
            if (!lastArgCanBeNull && exactLength)
            {
                return (args.Length == expectedLength && args[^1] != null);
            }
            if (!lastArgCanBeNull)
            {
                return (args.Length >= expectedLength && args[^1] != null);
            }
            else
            {
                return (args.Length >= expectedLength );
            }
        }

        private int InstallMap(string mapId)
        {
            var args = new string[]{"load", mapId};
            var installer = new MapInstaller(args, ref _state);
            return installer.PerformLoad();
        }
    }
}
