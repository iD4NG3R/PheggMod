﻿#pragma warning disable CS0626 // orig_ method is marked external and has no attributes on it.
using Mirror;
using MonoMod;
using PheggMod.API.Events;

namespace PheggMod.EventTriggers
{
    [MonoModPatch("global::DecontaminationLCZ")]
    class PMDecontaminationLCZ : DecontaminationLCZ
    {
        public extern void orig_RpcPlayAnnouncement(int id, bool global);
        public new void RpcPlayAnnouncement(int id, bool global)
        {
            orig_RpcPlayAnnouncement(id, global);

            if (this.GetOption("decontstart", id))
            {
                if (NetworkServer.active)
                {
                    PluginManager.TriggerEvent<IEventHandlerLczDecontaminate>(new LczDecontaminateEvent());
                }
            }
        }

        public extern bool orig_GetOption(string optionName, int curAnm);
        public bool GetOption(string optionName, int curAnm)
        {
            return orig_GetOption(optionName, curAnm);
        }
    }
}
