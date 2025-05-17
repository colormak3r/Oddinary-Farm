using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Any object that inherits from this interface can have their controllability toggled
public interface IControllable
{
    public void SetControllable(bool value);
}
