using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameClient : MonoBehaviour
{
    public static GameClient Instance { get; protected set; }

    public UserInfo UserInfo;

    public GameMode Game { get; private set; }

    protected virtual void Awake()
    {
        Install();
    }
    
    private void Install()
    {
        Instance = this;
    }
    
    public void StartGame(GameMode game)
    {
        if (Game != null)
        {
            Game.Stop();
            Game.Dispose();
        }

        Game = game;
        Game.Initiallize();
        Game.Run();

        System.GC.Collect();
    }
}
