﻿using Bencodex.Types;
using LastHandStanding.Exceptions;
using LastHandStanding.States;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace LastHandStanding.Actions;

[ActionType("CreateSession")]
public class CreateSession : ActionBase
{
    public CreateSession(Address sessionId, Address prize)
    {
        SessionId = sessionId;
        Prize = prize;
    }
    
    protected override void LoadPlainValueInternal(IValue plainValueInternal)
    {
        if (plainValueInternal is not List list)
        {
            throw new CreateSessionException("Given plainValue for CreateSession is not list");
        }
        
        SessionId = new Address(list[0]);
        Prize = new Address(list[1]);
    }

    public override IWorld Execute(IActionContext context)
    {
        var world = context.PreviousState;
        var sessionAccount = world.GetAccount(Addresses.Sessions);
        if (sessionAccount.GetState(SessionId) is not null)
        {
            throw new CreateSessionException($"Session of id {SessionId} already exists.");
        }

        var gloveAccount = world.GetAccount(Addresses.Gloves);
        if (gloveAccount.GetState(Prize) is not { } rawGloveState)
        {
            throw new CreateSessionException(
                $"Given glove prize {Prize} for session id {SessionId} does not exist.");
        }

        var glove = new Glove(rawGloveState);
        if (!glove.Author.Equals(context.Signer))
        {
            throw new CreateSessionException(
                $"Organizer for session id {SessionId} is not author of the prize {Prize}.");
        }
        
        var session = new Session(SessionId, context.Signer, Prize);

        sessionAccount = sessionAccount.SetState(SessionId, session.Bencoded);
        return world.SetAccount(Addresses.Sessions, sessionAccount);
    }

    protected override IValue PlainValueInternal => SessionId.Bencoded;

    public Address SessionId { get; private set; }

    public Address Prize { get; private set; }
}