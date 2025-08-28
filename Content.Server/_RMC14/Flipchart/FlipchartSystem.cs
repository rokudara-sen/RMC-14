using System;
using Content.Shared._RMC14.Flipchart;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Robust.Shared.Localization;

namespace Content.Server._RMC14.Flipchart;

public sealed class FlipchartSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlipchartComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FlipchartComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnInit(Entity<FlipchartComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Pages.Count == 0)
            ent.Comp.Pages.Add(string.Empty);
        ent.Comp.CurrentPage = Math.Clamp(ent.Comp.CurrentPage, 0, ent.Comp.Pages.Count - 1);

        if (TryComp<PaperComponent>(ent, out var paper))
            _paper.SetContent((ent.Owner, paper), ent.Comp.Pages[ent.Comp.CurrentPage]);
    }

    private void OnVerb(Entity<FlipchartComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("flipchart-verb-next-page"),
            Act = () => NextPage(ent)
        });
    }

    private void NextPage(Entity<FlipchartComponent> ent)
    {
        if (!TryComp<PaperComponent>(ent, out var paper))
            return;

        if (ent.Comp.CurrentPage >= 0 && ent.Comp.CurrentPage < ent.Comp.Pages.Count)
            ent.Comp.Pages[ent.Comp.CurrentPage] = paper.Content;

        ent.Comp.CurrentPage++;
        if (ent.Comp.CurrentPage >= ent.Comp.Pages.Count)
            ent.Comp.Pages.Add(string.Empty);

        _paper.SetContent((ent.Owner, paper), ent.Comp.Pages[ent.Comp.CurrentPage]);
        Dirty(ent);
    }
}
