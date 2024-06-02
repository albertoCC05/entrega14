using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public RoomSO currentRoom;

    private List<ItemSO> itemsInRoom = new List<ItemSO>();
    private List<string> itemDescriptionsInRoom = new List<string>();
    private List<ItemSO> usableItems = new List<ItemSO>();
    

    private Dictionary<string, RoomSO> exitsDictionary = new Dictionary<string, RoomSO>();
    private Dictionary<string, string> examineDictionary = new Dictionary<string, string>();
    private Dictionary<string, string> takeDictionary = new Dictionary<string, string>();
    private Dictionary<string, ActionResponseSO> useDictionary = new Dictionary<string, ActionResponseSO>();


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is more than one instance");
        }

        Instance = this;
    }

    public List<string> GetExitDescriptionsInRoom()
    {
        List<string> exitDescriptions = new List<string>();
        foreach (Exit exit in currentRoom.exits)
        {
            exitDescriptions.Add(exit.description);
            exitsDictionary.Add(exit.direction, exit.room);
        }

        return exitDescriptions;
    }

    private void SetItemsInRoom()
    {
        foreach (ItemSO item in currentRoom.items)
        {
            if (!Inventory.Instance.IsItemInInventory(item.itemName))
            {
                itemsInRoom.Add(item);
                itemDescriptionsInRoom.Add(item.description);
            }

            foreach (Interaction interaction in item.interactions)
            {
                if (interaction.inputAction.keyWord.Equals("examinar"))
                {
                    examineDictionary.Add(item.itemName, interaction.responseDescription);
                }
                else if (interaction.inputAction.keyWord.Equals("coger"))
                {
                    takeDictionary.Add(item.itemName, interaction.responseDescription);
                }
                else if (interaction.inputAction.keyWord.Equals("usar"))
                {
                    if (!usableItems.Contains(item))
                    {
                        usableItems.Add(item);
                    }
                }
            }
        }
    }
    
    public List<string> GetItemDescriptionsInRoom()
    {
        SetItemsInRoom();
        return itemDescriptionsInRoom;
    }

    public void TryToChangeRoom(string direction)
    {
        if (exitsDictionary.TryGetValue(direction, out var room))
        {
            GameManager.Instance.UpdateLogList($"vas hacia el {direction}");
            ChangeRoom(room);
        }
        else
        {
            GameManager.Instance.UpdateLogList($"No hay camino en direccion {direction}");
        }
    }

    public string TryToExamineItem(string item)
    {
        // TODO: Comprobar si también está en el inventario y es examinable
        if (examineDictionary.ContainsKey(item))
        {
            return examineDictionary[item];
        }

        return $"You can't examine {item}";
    }
    
    public string TryToTakeItem(string item)
    {
        if (takeDictionary.ContainsKey(item))
        {
            RemoveItemFromRoom(GetItemInRoomFromName(item));
            Inventory.Instance.AddItemToInventory(item);
            SetUseDictionary();
            return takeDictionary[item];
        }

        return $"You can't take {item}";
    }

    public void TryToUseItem(string itemToUse)
    {
        if (Inventory.Instance.IsItemInInventory(itemToUse))
        {
            if (useDictionary.TryGetValue(itemToUse, out ActionResponseSO actionResponse))
            {
                bool actionResult = actionResponse.DoActionResponse();
                if (!actionResult)
                {
                    GameManager.Instance.UpdateLogList("No pasa nada...");
                }
            }
            else
            {
                GameManager.Instance.UpdateLogList($"No puedes usar {itemToUse}");
            }
        }
        else
        {
            GameManager.Instance.UpdateLogList($"No hay ningun {itemToUse} en tu inventario para usar");
        }
    }

    private void SetUseDictionary()
    {
        foreach (string itemInInventory in Inventory.Instance.GetInventory())
        {
            ItemSO item = GetUsableItemFromName(itemInInventory);
            if (item == null)
            {
                continue;
            }

            foreach (Interaction interaction in item.interactions)
            {
                // Se supone que solo una interaction (y solo una) va a tener actionResponse (asociada a inputAction use)
                if (interaction.actionResponse == null)
                {
                    continue;
                }
                
                // Se supone que si llegamos a esta condición, estamos con la interaction de use
                if (!useDictionary.ContainsKey(itemInInventory))
                {
                    useDictionary.Add(itemInInventory, interaction.actionResponse);
                }
            }
        }
    }

    public void ClearExits()
    {
        exitsDictionary.Clear();
    }

    public void ClearItems()
    {
        itemsInRoom.Clear();
        itemDescriptionsInRoom.Clear();
        examineDictionary.Clear();
        takeDictionary.Clear();
    }

    private ItemSO GetItemInRoomFromName(string itemName)
    {
        foreach (ItemSO item in currentRoom.items)
        {
            if (itemName.Equals(item.itemName))
            {
                return item;
            }
        }

        return null;
    }
    
    private ItemSO GetUsableItemFromName(string itemName)
    {
        foreach (ItemSO item in usableItems)
        {
            if (itemName.Equals(item.itemName))
            {
                return item;
            }
        }

        return null;
    }

    private void RemoveItemFromRoom(ItemSO item)
    {
        itemsInRoom.Remove(item);
    }

    public void ChangeRoom(RoomSO newRoom)
    {
        currentRoom = newRoom;
        GameManager.Instance.DisplayFullRoomText();
    }
    
}
