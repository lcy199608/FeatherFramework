using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum FSMStateType
{
    Idle, Patrol, Seek, Attack,LookAt, Hit, Death
}

public class FSM : MonoBehaviour
{
    private List<IState> currentState = new List<IState>();
    protected Dictionary<FSMStateType, IState> states = new Dictionary<FSMStateType, IState>();

    protected void InitFSM(Dictionary<FSMStateType, IState> states, List<FSMStateType> startState)
    {
        this.states = states;
        TransitionState(startState);
    }

    void Update()
    {
        if(currentState.Count != 0)
        {
            for (int i = 0; i < currentState.Count; i++)
            {
                currentState[i].OnUpdate();
            }
        }
    }

    public void TransitionState(List<FSMStateType> type)
    {
        if (currentState != null)
            currentState.ForEach(_ => _.OnExit());

        currentState.Clear();
        List<IState> stateList = new List<IState>();
        type.ForEach(_ => stateList.Add(states[_]));
        currentState = stateList;

        currentState.ForEach(_ => _.OnEnter());
    }
}