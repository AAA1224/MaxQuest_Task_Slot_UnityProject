using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Global
{
   
    public static PlayerProfile playerProfile = new PlayerProfile();
    public static string jsonPath = "Prefabs/jsonData/";
    public static string itemImagePath = "Prefabs/textures/item/";
    public static string parachuteImagePath = "Prefabs/textures/parachute/";

    public static bool createdSocket = false;
    
}

[SerializeField]
public class PlayerProfile
{
    public int id;
    public string username;
    public string email;
    public int balance;
    public string selectedCID;
    public int coin;
    public int gem;
    public int egg;
}