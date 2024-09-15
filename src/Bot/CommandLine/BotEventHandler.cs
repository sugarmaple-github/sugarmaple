namespace Sugarmaple.Bot.CommandLine;
using Sugarmaple.TheSeed.Api;
using System;

public class BotEventHandler : IDisposable
{
    private readonly SeedBot _bot;
    private Action? _removeEvent;

    public SeedBot Bot => _bot;

    public BotEventHandler(SeedBot bot)
    {
        _bot = bot;
    }

    public void RemoveEvent()
    {
        _removeEvent?.Invoke();
    }

    public void Dispose() => RemoveEvent();

    public event Action<string, string> OnGetEditSuccessfully
    {
        add
        {
            _bot.GotEditSuccessfully += value;
            _removeEvent += () => _bot.GotEditSuccessfully -= value;
        }
        remove { _bot.GotEditSuccessfully -= value; }
    }

    //public event Action<EditPostResult> OnPostSuccessfully
    //{
    //    add
    //    {
    //        _bot.OnPostSuccessfully.Event += value;
    //        _removeEvent += () => _bot.OnPostSuccessfully.Event -= value;
    //    }
    //    remove { _bot.OnPostSuccessfully.Event -= value; }
    //}

    public event Action<EditPostError> OnPostEditError
    {
        add
        {
            _bot.PostedEditError += value;
            _removeEvent += () => _bot.PostedEditError -= value;
        }
        remove { _bot.PostedEditError -= value; }
    }

}
