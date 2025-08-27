using Content.Server.EUI;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed class MutineerInviteEui : BaseEui
{
    private readonly EntityUid _target;
    private readonly MutinySystem _mutiny;

    public MutineerInviteEui(EntityUid target, MutinySystem mutiny)
    {
        _target = target;
        _mutiny = mutiny;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not MutineerInviteChoiceMessage choice ||
            choice.Button != MutineerInviteUiButton.Accept)
        {
            Close();
            return;
        }

        _mutiny.MakeMutineer(_target);
        Close();
    }
}
