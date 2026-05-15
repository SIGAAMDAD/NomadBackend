using UnityEngine;
using Nomad.Core.Logger;

public class ExampleClass : MonoBehavior
{
    public void Awake()
    {
        // Log some messages...
        LoggerService.PrintLine();
    }
}