using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tdapi = Telegram.Td.Api;
using tdlib = Telegram.Td;

namespace TDLibCore
{
    public class TDLibCore : IDisposable
    {
        #region properties

        public string phonenumber { get; set; }
        public Helper hpcore { get; set; }
        private tdlib.Client client { get; set; }
        private Reshandler mainhandler { get; set; }
        public enums.ConnectionState connectionstate { get; set; }
        public enums.AuhtorizationState authorizationstate { get; set; }
        private Dictionary<string, Action<tdapi.BaseObject, Reshandler>> commands { get; set; }
        private object lockerobject { get; set; }
        private List<tdapi.Chat> mainchatslist { get; set; }
        public Dictionary<Type, EventHandler<TDLibCoreEventArgs>> mainresponsehandlers { get; set; }

        #endregion properties

        public TDLibCore(Helper hp)
        {
            this.hpcore = hp;
            lockerobject = new object();
            commands = new Dictionary<string, Action<tdapi.BaseObject, Reshandler>>();
            mainresponsehandlers = new Dictionary<Type, EventHandler<TDLibCoreEventArgs>>();
            this.authorizationstate = enums.AuhtorizationState.BackgroundActions;
            this.connectionstate = enums.ConnectionState.Updating;

            #region handle UpdateAuthorizationState,UpdateOption,UpdateConnectionState

            mainresponsehandlers.Add(new tdapi.UpdateAuthorizationState().GetType(), (a, b) =>
            {
                tdapi.UpdateAuthorizationState ustate = b.additionalobject as tdapi.UpdateAuthorizationState;
                tdapi.AuthorizationState state = ustate.AuthorizationState;
                if (state is tdapi.AuthorizationStateClosed)
                {
                    authorizationstate = enums.AuhtorizationState.Closed;
                }
                else if (state is tdapi.AuthorizationStateClosing)
                {
                    authorizationstate = enums.AuhtorizationState.Closing;
                }
                else if (state is tdapi.AuthorizationStateLoggingOut)
                {
                    authorizationstate = enums.AuhtorizationState.LoggingOut;
                }
                else if (state is tdapi.AuthorizationStateReady)
                {
                    b.core.OnReadyAction(new TDLibCoreEventArgs()
                    {
                        core = b.core
                    });
                    authorizationstate = enums.AuhtorizationState.Ready;
                }
                else if (state is tdapi.AuthorizationStateWaitEncryptionKey)
                {
                    client.Send(new tdapi.CheckDatabaseEncryptionKey(), null);
                    if (b.core.hpcore.debuglevel == enums.DebugLevel.Full || b.core.hpcore.debuglevel == enums.DebugLevel.Normal)
                    {
                        string[] split = b.core.hpcore.debugproxy.Split(':');
                        string proxyip = split[0];
                        int proxyport = int.Parse(split[1]);
                        client.Send(new tdapi.AddProxy(proxyip, proxyport, true, new tdapi.ProxyTypeHttp()), null);
                    }
                    else
                    {
                        client.Send(new tdapi.DisableProxy(), null);
                    }
                    authorizationstate = enums.AuhtorizationState.BackgroundActions;
                }
                else if (state is tdapi.AuthorizationStateWaitOtherDeviceConfirmation)
                {
                    authorizationstate = enums.AuhtorizationState.BackgroundActions;
                }
                else if (state is tdapi.AuthorizationStateWaitCode)
                {
                    authorizationstate = enums.AuhtorizationState.WaitingForVerificationCode;
                    b.core.OnVerificationCodeNeededAction(new TDLibCoreEventArgs()
                    {
                        core = b.core
                    });
                }
                else if (state is tdapi.AuthorizationStateWaitPassword)
                {
                    authorizationstate = enums.AuhtorizationState.WaitingForVerificationPassword;
                    b.core.OnVerificationPasswordNeededAction(new TDLibCoreEventArgs()
                    {
                        core = b.core
                    });
                }
                else if (state is tdapi.AuthorizationStateWaitPhoneNumber)
                {
                    client.Send(new tdapi.SetAuthenticationPhoneNumber(b.core.phonenumber, new tdapi.PhoneNumberAuthenticationSettings()
                    {
                        AllowFlashCall = true,
                        AllowSmsRetrieverApi = true
                    }), null);
                    authorizationstate = enums.AuhtorizationState.BackgroundActions;
                }
                else if (state is tdapi.AuthorizationStateWaitTdlibParameters)
                {
                    client.Send(new tdapi.SetTdlibParameters(new tdapi.TdlibParameters()
                    {
                        ApiHash = b.core.hpcore.APIHASH,
                        ApiId = b.core.hpcore.APIID,
                        ApplicationVersion = "1.0.0",
                        DeviceModel = "Desktop",
                        EnableStorageOptimizer = true,
                        SystemLanguageCode = "en",
                        UseSecretChats = true,
                        UseMessageDatabase = true,
                        UseChatInfoDatabase = true,
                        DatabaseDirectory = $"TDLibCoreData-{   b.core.phonenumber.Replace("+", "0")}"
                    }), null);
                    authorizationstate = enums.AuhtorizationState.BackgroundActions;
                }
                else
                {
                    b.core.authorizationstate = enums.AuhtorizationState.InvalidData;
                }
            });
            mainresponsehandlers.Add(new tdapi.UpdateConnectionState().GetType(), (a, b) =>
            {
                tdapi.UpdateConnectionState uct = b.additionalobject as tdapi.UpdateConnectionState;
                if (uct.State is tdapi.ConnectionStateConnecting)
                {
                    b.core.connectionstate = enums.ConnectionState.Connecting;
                }
                else if (uct.State is tdapi.ConnectionStateConnectingToProxy)
                {
                    b.core.connectionstate = enums.ConnectionState.ConnectingToProxy;
                }
                else if (uct.State is tdapi.ConnectionStateReady)
                {
                    b.core.connectionstate = enums.ConnectionState.Connected;
                }
                else if (uct.State is tdapi.ConnectionStateUpdating)
                {
                    b.core.connectionstate = enums.ConnectionState.Updating;
                }
                else if (uct.State is tdapi.ConnectionStateWaitingForNetwork)
                {
                    b.core.connectionstate = enums.ConnectionState.WaitingForNetwork;
                }
                else
                {
                    b.core.connectionstate = enums.ConnectionState.InvalidData;
                }
            });
            mainresponsehandlers.Add(new tdapi.UpdateOption().GetType(), (a, b) =>
            {
                tdapi.UpdateOption upn = b.additionalobject as tdapi.UpdateOption;
                client.Send(new tdapi.SetOption(upn.Name, upn.Value), null);
            });

            #endregion handle UpdateAuthorizationState,UpdateOption,UpdateConnectionState
        }

        /// <summary>
        /// Initializes TDLibCore instance and run new telegram client
        /// </summary>
        [Obsolete]
        public void initializeclient()
        {
            switch (hpcore.debuglevel)
            {
                case enums.DebugLevel.Full:
                    string lgfilename = $"{phonenumber}-log.txt";
                    if (System.IO.File.Exists(lgfilename))
                        System.IO.File.Delete(lgfilename);
                    tdlib.Log.SetFilePath(lgfilename);
                    break;

                default:
                    tdlib.Log.SetVerbosityLevel(0);
                    break;
            }

            mainhandler = new Reshandler(this);
            ReadCommands(mainhandler);
            client = tdlib.Client.Create(mainhandler);
            Task.Factory.StartNew(() =>
            {
                client.Run();
            });
            hpcore.addlog($"{phonenumber} client initialization successed");
        }

        /// <summary>
        /// Sends input data for authentication purpos if authorization status matches
        /// </summary>
        /// <param name="input">this can be verification password or code</param>
        /// <returns>request response</returns>
        public async Task<(enums.Response response, object responseobj)> Authenticate(string input)
        {
            enums.Response res = enums.Response.Processing;
            object data = new object();
            if (this.authorizationstate == enums.AuhtorizationState.WaitingForVerificationCode)
            {
                Responseobject ChecAuthenticationCoderesponse = await ExecuteCommandAsync(new tdapi.CheckAuthenticationCode(input), new tdapi.Ok());
                if (ChecAuthenticationCoderesponse.response == enums.Response.Success)
                {
                    res = enums.Response.Success;
                }
                else
                {
                    authorizationstate = enums.AuhtorizationState.Failed;
                    res = ChecAuthenticationCoderesponse.response;
                    data = ChecAuthenticationCoderesponse.responseobject;
                }
            }
            else if (this.authorizationstate == enums.AuhtorizationState.WaitingForVerificationPassword)
            {
                Responseobject ChecAuthenticationPasswordresponse = await ExecuteCommandAsync(new tdapi.CheckAuthenticationPassword(input), new tdapi.Ok());
                if (ChecAuthenticationPasswordresponse.response == enums.Response.Success)
                {
                    res = enums.Response.Success;
                }
                else
                {
                    authorizationstate = enums.AuhtorizationState.Failed;
                    res = ChecAuthenticationPasswordresponse.response;
                    data = ChecAuthenticationPasswordresponse.responseobject;
                }
            }
            else
            {
                res = enums.Response.Success;
                data = null;
                hpcore.addlog($"[Authenticate] - authorizationstate is {authorizationstate.ToString()}");
            }
            return (res, data);
        }

        /// <summary>
        /// current object disposal
        /// </summary>
        public void Dispose()
        {
            GC.Collect();
            GC.SuppressFinalize(this);
        }

        #region easy-to-use functions

        /// <summary>
        /// Get authentication phonenumber main chats
        /// </summary>
        /// <returns>A list which contains tdpi.Chat instnaces</returns>
        public async Task<List<tdapi.Chat>> GetMainChatList()
        {
            List<tdapi.Chat> res = new List<tdapi.Chat>();
            long offsetorder = long.MaxValue;
            long offsetid = 0;
            int max = 100000;
            while (true)
            {
                Responseobject GetChatListresponse = await ExecuteCommandAsync(new tdapi.GetChats(null, offsetorder, offsetid, max), new tdapi.Chats());
                if (GetChatListresponse.response == enums.Response.Success)
                {
                    tdapi.Chats chats = GetChatListresponse.responseobject as tdapi.Chats;
                    if (chats.ChatIds.Length > 0)
                    {
                        foreach (long item in chats.ChatIds)
                        {
                            Responseobject GetChatresponse = await ExecuteCommandAsync(new tdapi.GetChat(item), new tdapi.Chat());
                            if (GetChatresponse.response == enums.Response.Success)
                            {
                                res.Add(GetChatresponse.responseobject as tdapi.Chat);
                            }
                            else
                            {
                                hpcore.addlog("[GetMainChatList][GetChatresponse] - " + GetChatListresponse.responseobject);
                            }
                        }
                        if (res.Count > 0)
                        {
                            tdapi.Chat last = res.Last();
                            offsetid = last.Id;
                            offsetorder = last.Positions.FirstOrDefault(Func => Func.List is tdapi.ChatListMain).Order;
                        }
                        hpcore.addlog($"[GetMainChatList] - gathered {res.Count:n0}");
                    }
                    else
                    {
                        hpcore.addlog($"[GetMainChatList] - finished, total {res.Count:n0}");
                        break;
                    }
                }
                else
                {
                    hpcore.addlog("[GetMainChatList][GetChatListresponse] - " + GetChatListresponse.responseobject);
                    break;
                }
            }
            res = res
                .GroupBy(func => func.Id)
                .Select(func => func.First())
                .Distinct()
                .ToList();
            mainchatslist = res;
            return mainchatslist;
        }

        /// <summary>
        /// returns a specified super group members if possible
        /// </summary>
        /// <param name="supergroupid">target super group identifier</param>
        /// <returns>A list which contains tdpi.User instnaces</returns>
        public async Task<List<tdapi.User>> GetSuperGroupUsers(long supergroupid)
        {
            hpcore.addlog("[GetSuperGroupUsers] - gathering " + supergroupid);
            List<tdapi.User> res = new List<tdapi.User>();
            List<tdapi.ChatMember> chatmemberslist = new List<tdapi.ChatMember>();
            Responseobject GetSuperGroupChatresponse = await ExecuteCommandAsync(new tdapi.GetChat(supergroupid), new tdapi.Chat());
            if (GetSuperGroupChatresponse.response == enums.Response.Success)
            {
                tdapi.Chat groupchat = GetSuperGroupChatresponse.responseobject as tdapi.Chat;
                if (groupchat.Type is tdapi.ChatTypeSupergroup supergroupchat)
                {
                    Responseobject GetSupergroupFullInforesponse = await ExecuteCommandAsync(new tdapi.GetSupergroupFullInfo(supergroupchat.SupergroupId), new tdapi.SupergroupFullInfo());
                    if (GetSupergroupFullInforesponse.response == enums.Response.Success)
                    {
                        tdapi.SupergroupFullInfo supergroupFullInfo = GetSupergroupFullInforesponse.responseobject as tdapi.SupergroupFullInfo;
                        if (supergroupFullInfo.CanGetMembers)
                        {
                            int max = 200;
                            while (true)
                            {
                                Responseobject GetSuperGroupMemberresponse = await ExecuteCommandAsync(new tdapi.GetSupergroupMembers(supergroupchat.SupergroupId, null, chatmemberslist.Count, max), new tdapi.ChatMembers());
                                if (GetSuperGroupMemberresponse.response == enums.Response.Success)
                                {
                                    tdapi.ChatMembers members = GetSuperGroupMemberresponse.responseobject as tdapi.ChatMembers;
                                    if (members.Members.Length > 0)
                                    {
                                        foreach (tdapi.ChatMember item in members.Members)
                                        {
                                            chatmemberslist.Add(item);
                                        }
                                        hpcore.addlog($"[GetSuperGroupMemberresponse] - gathered {chatmemberslist.Count:n0}");
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    hpcore.addlog("[GetSuperGroupMemberresponse] - " + GetSuperGroupMemberresponse.responseobject);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        hpcore.addlog($"[GetSuperGroupUsers][GetSupergroupFullInforesponse] - response is {GetSupergroupFullInforesponse.responseobject}");
                    }
                }
                else
                {
                    hpcore.addlog($"[GetSuperGroupUsers][GetSupergroupChatresponse] - target group is {groupchat.Type.GetType()}");
                }
            }
            else
            {
                hpcore.addlog($"[GetSuperGroupUsers][GetSupergroupChatresponse] - response is {GetSuperGroupChatresponse}");
            }
            if (chatmemberslist.Count > 0)
            {
                hpcore.addlog($"[GetSuperGroupUsers][GetUser] -  for {chatmemberslist.Count:n0}");
                List<Task> tklist = new List<Task>();
                foreach (tdapi.ChatMember item in chatmemberslist)
                {
                    tklist.Add(Task.Run(async () =>
                    {
                        Responseobject itemuser = await ExecuteCommandAsync(new tdapi.GetUser(item.UserId), new tdapi.User());
                        if (itemuser.response == enums.Response.Success)
                        {
                            lock (lockerobject)
                            {
                                res.Add(itemuser.responseobject as tdapi.User);
                            }
                        }
                    }));
                }
                await Task.WhenAll(tklist);
            }

            return res
                    .GroupBy(func => func.Id)
                    .Select(func => func.First())
                    .Distinct()
                    .ToList();
        }

        /// <summary>
        /// returns a specified super group members if possible
        /// </summary>
        /// <param name="groupidentifier">target super group search query</param>
        /// <param name="istitle">true if query contains in target group title</param>
        /// <returns>A list which contains tdpi.User instnaces</returns>
        public async Task<List<tdapi.User>> GetSuperGroupUsers(string groupidentifier, bool istitle = false)
        {
            List<tdapi.User> res = new List<tdapi.User>();
            if (istitle)
            {
                if (mainchatslist is null)
                {
                    await GetMainChatList();
                }
                tdapi.Chat target = mainchatslist.FirstOrDefault(Func => Func.Title.ToLower().IndexOf(groupidentifier.ToLower()) >= 0);
                if (target != null)
                {
                    res = await GetSuperGroupUsers(target.Id);
                }
                else
                {
                    hpcore.addlog("[GetSuperGroupUsers][searchmainchatlist] - chat not found");
                }
            }
            else
            {
                Responseobject search = await ExecuteCommandAsync(new tdapi.SearchPublicChat(groupidentifier));
                if (search.response == enums.Response.Success)
                {
                    tdapi.Chat target = search.responseobject as tdapi.Chat;
                    res = await GetSuperGroupUsers(target.Id);
                }
                else
                {
                    hpcore.addlog("[GetSuperGroupUsers][search] - " + search);
                }
            }
            return res;
        }

        /// <summary>
        /// asynchronously run a tdapi.Function
        /// </summary>
        /// <param name="func">function you want to run asynchronously</param>
        /// <param name="expectedtype">the type which you expect your function response receives</param>
        /// <returns>A Responseobject contains your task information</returns>
        public async Task<Responseobject> ExecuteCommandAsync(tdapi.Function func, tdapi.BaseObject expectedtype = null)
        {
            Responseobject res = new Responseobject();
            Dictionary<string, Action<tdapi.BaseObject, Reshandler>> customcommands = new Dictionary<string, Action<tdapi.BaseObject, Reshandler>>();
            customcommands.Add("Custom", (a, b) =>
            {
                b.responseobject = a;
                if (expectedtype is null)
                {
                    b.response = enums.Response.Success;
                }
                else
                {
                    if (a.GetType() == expectedtype.GetType())
                    {
                        b.response = enums.Response.Success;
                    }
                    else
                    {
                        b.response = enums.Response.Failed;
                    }
                }
            });
            Reshandler handler = new Reshandler(this)
            {
                command = "Custom",
            };
            ReadCommands(handler, customcommands);
            client.Send(func, handler);
            res = await handler.getresponse();
            return res;
        }

        /// <summary>
        /// Adds a user in specified super group
        /// </summary>
        /// <param name="user">target user tdapi.User instnace</param>
        /// <param name="target">target group tdapi.Chat instnace</param>
        /// <param name="addall">if true, user will be added even if its blocked, or last seen long time ago</param>
        public async Task<(enums.Response response, object obj)> AddChatMember(tdapi.User user, tdapi.Chat target, bool addall = false)
        {
            enums.Response res = new enums.Response();
            object obj = null;
            if (user.Username is null || user.Username.Length <= 0)
            {
                res = enums.Response.Failed;
                obj = new tdapi.Error()
                {
                    Message = "User username cannot be empty"
                };
            }
            else
            {
                if (addall)
                {
                    Responseobject SearchPublicChatresponse = await ExecuteCommandAsync(new tdapi.SearchPublicChat(user.Username), new tdapi.Chat());
                    if (SearchPublicChatresponse.response == enums.Response.Success)
                    {
                        Responseobject CreatePrivateChatresposnse = await ExecuteCommandAsync(new tdapi.CreatePrivateChat(user.Id, false), new tdapi.Chat());
                        if (CreatePrivateChatresposnse.response == enums.Response.Success)
                        {
                            Responseobject AddChatMemberresponse = await ExecuteCommandAsync(new tdapi.AddChatMember(target.Id
                                , user.Id, 100), new tdapi.Ok());
                            res = AddChatMemberresponse.response;
                            obj = AddChatMemberresponse.responseobject;
                        }
                        else
                        {
                            res = CreatePrivateChatresposnse.response;
                            obj = CreatePrivateChatresposnse.responseobject;
                        }
                    }
                    else
                    {
                        res = SearchPublicChatresponse.response;
                        obj = SearchPublicChatresponse.responseobject;
                    }
                }
                else
                {
                    if (user.Status is tdapi.UserStatusLastWeek || user.Status is tdapi.UserStatusLastMonth || user.Status is tdapi.UserStatusEmpty)
                    {
                        res = enums.Response.Failed;
                        obj = new tdapi.Error()
                        {
                            Message = "Username last seen is " + user.Status.GetType()
                        };
                    }
                    else
                    {
                        Responseobject SearchPublicChatresponse = await ExecuteCommandAsync(new tdapi.SearchPublicChat(user.Username), new tdapi.Chat());
                        if (SearchPublicChatresponse.response == enums.Response.Success)
                        {
                            Responseobject CreatePrivateChatresposnse = await ExecuteCommandAsync(new tdapi.CreatePrivateChat(user.Id, false), new tdapi.Chat());
                            if (CreatePrivateChatresposnse.response == enums.Response.Success)
                            {
                                Responseobject AddChatMemberresponse = await ExecuteCommandAsync(new tdapi.AddChatMember(target.Id
                                    , user.Id, 100), new tdapi.Ok());
                                res = AddChatMemberresponse.response;
                                obj = AddChatMemberresponse.responseobject;
                            }
                            else
                            {
                                res = CreatePrivateChatresposnse.response;
                                obj = CreatePrivateChatresposnse.responseobject;
                            }
                        }
                        else
                        {
                            res = SearchPublicChatresponse.response;
                            obj = SearchPublicChatresponse.responseobject;
                        }
                    }
                }
            }

            return (res, obj);
        }

        #endregion easy-to-use functions

        #region commands modification

        private void ReadCommands(Reshandler handler, Dictionary<string, Action<tdapi.BaseObject, Reshandler>> customcommands = null)
        {
            if (customcommands is null)
                handler.SetCommands(this.commands);
            else
                handler.SetCommands(customcommands);
        }

        #endregion commands modification

        #region events

        public event EventHandler<TDLibCoreEventArgs> OnVerificationCodeNeeded;

        public event EventHandler<TDLibCoreEventArgs> OnVerificationPasswordNeeded;

        public event EventHandler<TDLibCoreEventArgs> OnReady;

        public virtual void OnVerificationCodeNeededAction(TDLibCoreEventArgs args)
        {
            if (OnVerificationCodeNeeded != null)
            {
                lock (lockerobject)

                    OnVerificationCodeNeeded(this, args);
            }
        }

        public virtual void OnVerificationPasswordNeededAction(TDLibCoreEventArgs args)
        {
            if (OnVerificationPasswordNeeded != null)
            {
                lock (lockerobject)
                    OnVerificationPasswordNeeded(this, args);
            }
        }

        public virtual void OnReadyAction(TDLibCoreEventArgs args)
        {
            if (OnReady != null)
            {
                OnReady(this, args);
            }
        }

        #endregion events
    }
}