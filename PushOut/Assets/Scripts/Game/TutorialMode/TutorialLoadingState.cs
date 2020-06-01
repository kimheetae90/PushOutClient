using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialLoadingState : FSMState
{
    private TutorialMode cachedMode;

    public override void Enter()
    {
        cachedMode = Base as TutorialMode;
        cachedMode.ActorPool.Initiallize(2);
        cachedMode.PushOutEffectPool.Initiallize(2);
        cachedMode.NicknamePool.Initiallize(2);

        ResourceLoader.Instance.Load("Devil/devil");

        cachedMode.server.Enter("player", 0.3f, 1.3f);
        Change("Tutorial");
    }

    public override void Dispose()
    {
        cachedMode = null;
    }
}
