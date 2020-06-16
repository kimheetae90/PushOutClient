public struct EventableInt
{
    private int value;

    public delegate void OnChangeDelegate(int inValue);
    public OnChangeDelegate OnChange;

    public int Get()
    {
        return value;
    }

    public void Set(int inValue)
    {
        value = inValue;
        if(OnChange != null)
            OnChange(inValue);
    }
}

public struct EventableLong
{
    private long value;

    public delegate void OnChangeDelegate(long inValue);
    public OnChangeDelegate OnChange;

    public long Get()
    {
        return value;
    }

    public void Set(long inValue)
    {
        value = inValue;
        if (OnChange != null)
            OnChange(inValue);
    }
}

public struct EventableFloat
{
    private float value;

    public delegate void OnChangeDelegate(float inValue);
    public OnChangeDelegate OnChange;

    public float Get()
    {
        return value;
    }

    public void Set(float inValue)
    {
        value = inValue;
        if (OnChange != null)
            OnChange(inValue);
    }
}

public struct EventableDouble
{
    private double value;

    public delegate void OnChangeDelegate(double inValue);
    public OnChangeDelegate OnChange;

    public double Get()
    {
        return value;
    }

    public void Set(double inValue)
    {
        value = inValue;
        if (OnChange != null)
            OnChange(inValue);
    }
}

public struct EventableBool
{
    private bool value;

    public delegate void OnChangeDelegate(bool inValue);
    public OnChangeDelegate OnChange;

    public bool Get()
    {
        return value;
    }

    public void Set(bool inValue)
    {
        value = inValue;
        if (OnChange != null)
            OnChange(inValue);
    }
}

public struct EventableString
{
    private string value;

    public delegate void OnChangeDelegate(string inValue);
    public OnChangeDelegate OnChange;

    public string Get()
    {
        return value;
    }

    public void Set(string inValue)
    {
        value = inValue;
        if (OnChange != null)
            OnChange(inValue);
    }
}

