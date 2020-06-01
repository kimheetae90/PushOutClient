public abstract class FSMState
{
    public string Name;
    public FSMBase Base;

    public void Initiallize(string name, FSMBase inBase)
    {
        Name = name;
        Base = inBase;
    }

    public virtual void Enter() { }
    public virtual void Stay() { }
    public virtual void Exit() { }
    public virtual void Dispose() { }

    public void Change(string stateName)
    {
        Base.Change(stateName);
    }
}
