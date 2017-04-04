﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Events;
using PlayGroup;
using Equipment;
using Cupboards;
using UI;

public class PlayerNetworkActions : NetworkBehaviour
{
    private Dictionary<string, GameObject> ServerCache = new Dictionary<string, GameObject>();
    private string[] eventNames = new string[]
    {"suit", "belt", "feet", "head", "mask", "uniform", "neck", "ear", "eyes", "hands",
        "id", "back", "rightHand", "leftHand", "storage01", "storage02", "suitStorage"
    };

    private Equipment.Equipment equipment;

    void Start()
    {
        equipment = GetComponent<Equipment.Equipment>();
    }

    public override void OnStartServer()
    {
        if (isServer)
        {
            foreach (string cacheName in eventNames)
            {
                ServerCache.Add(cacheName, null);
            }
        }
        base.OnStartServer();
    }

    //This is only called from interaction on the client (from PickUpTrigger)
    public bool TryToPickUpObject(GameObject itemObject)
    {            
        if (PlayerManager.PlayerScript != null)
        {
            if (!isLocalPlayer)
                return false;
				
            if (!UIManager.Hands.CurrentSlot.TrySetItem(itemObject))
            {
                return false;
            }
            else
            {
                CmdTryToPickUpObject(UIManager.Hands.CurrentSlot.eventName, itemObject);
            }
        }
        else
        {
            return false;
        }
        return true;
    }

    //Server only (from Equipment Initial SetItem method
    public void TrySetItem(string eventName, GameObject obj)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null)
            {
                ServerCache[eventName] = obj;
                RpcTrySetItem(eventName, obj);
            }
        }
    }

    [Command]
    public void CmdTryToPickUpObject(string eventName, GameObject obj)
    {			
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null)
            {
                ServerCache[eventName] = obj;
                equipment.SetHandItem(eventName, obj);
            }
        }
    }

    [ClientRpc]
    public void RpcTrySetItem(string eventName, GameObject obj)
    {
        if (isLocalPlayer)
        {
            if (eventName.Length > 0)
            {
                EventManager.UI.TriggerEvent(eventName, obj);
            }
        }
    }

    [Command]
    public void CmdDropItem(string eventName)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName]);
                RpcAdjustItemParent(ServerCache[eventName], null);
                ServerCache[eventName] = null;
                equipment.ClearItemSprite(eventName);
            }
        }
    }

    [Command]
    public void CmdPlaceItem(string eventName, Vector3 pos, GameObject newParent)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                GameObject item = ServerCache[eventName];
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName], pos);
                ServerCache[eventName] = null;
                item.transform.parent = newParent.transform;
                RpcAdjustItemParent(item, newParent);
                equipment.ClearItemSprite(eventName);
            }
        }
    }

    [Command]
    public void CmdPlaceItemCupB(string eventName, Vector3 pos, GameObject newParent)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                GameObject item = ServerCache[eventName];
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName], pos);
                ServerCache[eventName] = null;
                ClosetControl closetCtrl = newParent.GetComponent<ClosetControl>();
                item.transform.parent = closetCtrl.items.transform;
                RpcAdjustItemParentCupB(item, newParent);
                equipment.ClearItemSprite(eventName);
            }
        }
    }

    [ClientRpc]
    void RpcAdjustItemParent(GameObject item, GameObject parent)
    {
        if (parent != null){
            item.transform.parent = parent.transform;
        } else {
            item.transform.parent = null;
        }
    }
    [ClientRpc]
    void RpcAdjustItemParentCupB(GameObject item, GameObject parent)
    {
        if (parent != null){
            ClosetControl closetCtrl = parent.GetComponent<ClosetControl>();
            item.transform.parent = closetCtrl.items.transform;
        } else {
            item.transform.parent = null;
        }
    }

    [Command]
    public void CmdToggleCupboard(GameObject cupbObj){
        ClosetControl closetControl = cupbObj.GetComponent<ClosetControl>();
        closetControl.ServerToggleCupboard();
    }
}