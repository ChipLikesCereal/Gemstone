using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gemstone.Gemstone
{
    public class JoinNotifs : MonoBehaviourPunCallbacks
    {
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            //base.OnPlayerEnteredRoom(newPlayer);
            NotiLib.SendNotification("<color=green>[JOIN] </color>" + newPlayer.NickName, 2000);
        }
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            //base.OnPlayerLeftRoom(otherPlayer);
            NotiLib.SendNotification("<color=red>[LEAVE] </color>" + otherPlayer.NickName, 2000);
        }
    }
}
