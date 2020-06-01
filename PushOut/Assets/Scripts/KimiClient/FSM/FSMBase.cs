using System.Collections.Generic;
using UnityEngine;

public abstract class FSMBase
{
    private Dictionary<string, FSMState> stateDic;
    public FSMState CurrentState { get; private set; }

    public abstract void Initiallize();
    public abstract void Dispose();

    public void Run()
    {
        if(stateDic.Count == 0)
        {
            Debug.LogError("[FSMBase]Insert FSMState!");
            return;
        }

        CurrentState.Enter();
        FSM.Instance.Add(this);
    }

    public void Stop()
    {
        CurrentState.Exit();
        FSM.Instance.ReserveDisposeFSMState(CurrentState);
        FSM.Instance.Remove(this);
    }

    public void AddState(string name, FSMState fsmState)
    {
        if(fsmState == null)
        {
            Debug.LogError("[FSMBase]FSM State is Null!");
            return;
        }

        if(stateDic == null)
        {
            stateDic = new Dictionary<string, FSMState>();
            CurrentState = fsmState;
        }

        fsmState.Name = name;
        fsmState.Base = this;

        stateDic.Add(fsmState.Name, fsmState);
    }

    public void Change(string key)
    {
        if(CurrentState == null)
        {
            Debug.LogWarning("[FSMBase]Don't use this function to start fsm!");
        }
        else
        {
            CurrentState.Exit();
            FSM.Instance.ReserveDisposeFSMState(CurrentState);
        }

        FSMState changeState = GetState(key);
        CurrentState = changeState;
        CurrentState.Enter();
    }

    public FSMState GetState(string key)
    {
        FSMState state = null;
        if(!stateDic.TryGetValue(key, out state))
        {
            Debug.LogWarning("[FSMBase]Doesn't Contain FSMState! key : " + key);
            return null;
        }

        return state;
    }
}
