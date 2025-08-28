using Content.Shared._RMC14.Flipchart;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.UserInterface;
using Robust.Shared.Localization;

namespace Content.Server._RMC14.Flipchart;

public sealed class FlipchartSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlipchartComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FlipchartComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<FlipchartComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FlipchartComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnInit(Entity<FlipchartComponent> ent, ref ComponentInit args)
    {
        ent.Comp.CurrentPage = 0;
    }

    private void OnVerb(Entity<FlipchartComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Pages.Count == 0)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("flipchart-verb-next-page"),
            Act = () => NextPage(ent, args.User)
        });
    }

    private void NextPage(Entity<FlipchartComponent> ent, EntityUid user)
    {
        if (!TryComp<PaperComponent>(ent, out var paper))
            return;

        if (ent.Comp.Pages.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("flipchart-popup-empty"), user, ent);
            return;
        }

        ent.Comp.Pages[ent.Comp.CurrentPage] = paper.Content;

        if (ent.Comp.CurrentPage >= ent.Comp.Pages.Count - 1)
        {
            _popup.PopupClient(Loc.GetString("flipchart-popup-no-pages"), user, ent);
            return;
        }

        ent.Comp.CurrentPage++;

        _paper.SetContent((ent.Owner, paper), ent.Comp.Pages[ent.Comp.CurrentPage]);
        Dirty(ent);
    }

    private void OnOpenAttempt(Entity<FlipchartComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.Pages.Count != 0)
            return;

        _popup.PopupClient(Loc.GetString("flipchart-popup-empty"), args.User, ent);
        args.Cancel();
    }

    private void OnInteractUsing(Entity<FlipchartComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PaperComponent>(args.Used, out var paper))
            return;

        if (!TryComp<PaperComponent>(ent, out var boardPaper))
            boardPaper = AddComp<PaperComponent>(ent.Owner);

        // insert after current page
        var index = ent.Comp.CurrentPage + 1;
        ent.Comp.Pages.Insert(index, paper.Content);
        ent.Comp.CurrentPage = index;
        _paper.SetContent((ent.Owner, boardPaper), paper.Content);
        Dirty(ent);

        QueueDel(args.Used);
        _popup.PopupClient(Loc.GetString("flipchart-popup-added"), args.User, ent);
        args.Handled = true;
    }
}
