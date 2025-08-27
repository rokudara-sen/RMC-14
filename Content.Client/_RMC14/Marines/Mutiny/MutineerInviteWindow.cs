using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Marines.Mutiny;

public sealed class MutineerInviteWindow : DefaultWindow
{
    public readonly Button DenyButton;
    public readonly Button AcceptButton;

    public MutineerInviteWindow()
    {
        Title = Loc.GetString("mutineer-invite-title");

        AcceptButton = new Button
        {
            Text = Loc.GetString("mutineer-invite-accept")
        };

        DenyButton = new Button
        {
            Text = Loc.GetString("mutineer-invite-deny")
        };

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        new Label
                        {
                            Text = Loc.GetString("mutineer-invite-text")
                        },
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Align = AlignMode.Center,
                            Children =
                            {
                                AcceptButton,
                                new Control
                                {
                                    MinSize = new Vector2(20, 0)
                                },
                                DenyButton
                            }
                        }
                    }
                }
            }
        });
    }
}
