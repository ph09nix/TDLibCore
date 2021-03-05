using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tdapi = Telegram.Td.Api;
using tdlib = Telegram.Td;

namespace TDLibCore
{
    public class Reshandler : tdlib.ClientResultHandler
    {
        #region properties

        private TDLibCore core { get; set; }
        public string command { get; set; }
        public enums.Response response { get; set; }
        public object responseobject { get; set; }
        private Dictionary<string, Action<tdapi.BaseObject, Reshandler>> commands { get; set; }

        #endregion properties

        public Reshandler(TDLibCore cr)
        {
            response = enums.Response.Processing;
            this.core = cr;
            if (command is null)
            {
                command = "";
            }
        }

        public void SetCommands(Dictionary<string, Action<tdapi.BaseObject, Reshandler>> cms)
        {
            this.commands = cms;
        }

        public void OnResult(tdapi.BaseObject @object)
        {
            if (commands.ContainsKey(command))
            {
                commands[command].Invoke(@object, this);
            }
            else
            {
                if (this.core.mainresponsehandlers.ContainsKey(@object.GetType()))
                {
                    Action<TDLibCoreEventArgs> handler = core.mainresponsehandlers[@object.GetType()];
                    if (handler != null)
                    {
                        handler(new TDLibCoreEventArgs()
                        {
                            core = core,
                            additionalobject = @object
                        });
                    }
                }
            }
        }

        public async Task<Responseobject> getresponse()
        {
            int sleep = 75;
            int timeout = core.hpcore.timeout / sleep;
            int tries = 0;
            while (response == enums.Response.Processing)
            {
                if (tries >= timeout)
                {
                    break;
                }
                await Task.Delay(sleep);
                tries++;
            }
            if (response == enums.Response.Processing)
            {
                return new Responseobject()
                {
                    response = enums.Response.TimedOut,
                    responseobject = null
                };
            }
            else
            {
                return new Responseobject()
                {
                    response = this.response,
                    responseobject = this.responseobject
                };
            }
        }
    }

    public class TDLibCoreEventArgs : EventArgs
    {
        public TDLibCore core { get; set; }
        public object additionalobject { get; set; }
    }

    public class Responseobject
    {
        public enums.Response response { get; set; }
        public object responseobject { get; set; }

        public Responseobject()
        {
            response = enums.Response.Processing;
        }
    }
}