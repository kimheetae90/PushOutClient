using System.Collections.Generic;
using UnityEngine;

public class FSM : MonoBehaviour
{
    public static FSM Instance;

    private List<FSMBase> fsmList;
    private List<FSMState> disposeFSMStateQue;

    private void Awake()
    {
        Install();
    }

    public void Install()
    {
        Instance = this;
        fsmList = new List<FSMBase>();
        disposeFSMStateQue = new List<FSMState>();
    }

    private void FixedUpdate()
    {
        foreach (var fsm in disposeFSMStateQue)
        {
            fsm.Dispose();
        }

        if(disposeFSMStateQue.Count > 0)
        {
            disposeFSMStateQue.Clear();
        }

        for(int i = 0; i < fsmList.Count;i++)
        {
            fsmList[i].Update();
            fsmList[i].CurrentState.Stay();
        }
    }

    public bool Add(FSMBase newFSM)
    {
        if(fsmList.Contains(newFSM))
        {
            Debug.LogError("Already Contain FSM!");
            return false;
        }

        fsmList.Add(newFSM);
        return true;
    }

    public bool Remove(FSMBase removeFSM)
    {
        if(!fsmList.Contains(removeFSM))
        {
            Debug.LogError("Doesn't Contain FSM!");
            return false;
        }

        fsmList.Remove(removeFSM);
        return true;
    }

    public void ReserveDisposeFSMState(FSMState inFSMState)
    {
        if (inFSMState != null)
        {
            disposeFSMStateQue.Add(inFSMState);
        }
    }
}
