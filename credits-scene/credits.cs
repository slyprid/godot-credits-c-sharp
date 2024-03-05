using Godot;
using Godot.Collections;
using System.Diagnostics;

public partial class credits : Control
{
    private VBoxContainer _scrollingContainer;
    private scrolling_text _scrollingText;
    private TextureRect _titleImage;
    private VideoStreamPlayer _backgroundVideo;
    private AudioStreamPlayer _musicPlayer;
    private Vector2 _viewSize;
    private int _regularSpeed;
    private bool _done;
    private bool _isFirstFrame = true;
    private FileAccess _file;
    private string _credits;
    private int _playlistIndex = 0;

    [Export] private string CreditsFile { get; set; }
    [Export] public int Speed { get; set; } = 40;
    [Export] public Texture2D TitleImage { get; set; }
    [Export] public Color BackgroundColor { get; set; } = Colors.Black;
    [Export] public VideoStream BackgroundVideo { get; set; }
    [Export] public bool LoopVideo { get; set; }
    [Export] public Color TitleColor { get; set; } = Colors.Gray;
    [Export] public Color NameColor { get; set; } = Colors.White;
    [Export] public Font CustomFont { get; set; }
    [Export] public int Margin { get; set; } = 6;
    [Export] public Array<AudioStream> MusicPlaylist { get; set; }
    [Export] public bool LoopPlaylist { get; set; } = false;
    [Export] public bool EnableControls { get; set; }
    [Export] public string SpeedUpControl { get; set; } = "ui_up";
    [Export] public string SlowDownControl { get; set; } = "ui_down";
    [Export] public bool EnableSkip { get; set; } = true;
    [Export] public string SkipControl { get; set; } = "ui_accept";
    [Export] public PackedScene NextScene { get; set; }
    [Export] public bool QuitOnEnd { get; set; }
    [Export] public bool DestroyOnEnd { get; set; }

    [Signal] public delegate void EndedEventHandler();

    public override void _Ready()
    {
        _scrollingContainer = GetNode<VBoxContainer>("scrollingContainer");
        _titleImage = GetNode<TextureRect>("scrollingContainer/titleImg");
        _backgroundVideo = GetNode<VideoStreamPlayer>("backgroundVideo");
        _musicPlayer = GetNode<AudioStreamPlayer>("musicPlayer");

        _viewSize = GetViewport().GetVisibleRect().Size;
        _scrollingContainer.Position = new Vector2(_scrollingContainer.Position.X, _viewSize.Y * 2);
        _regularSpeed = Speed;

        if (TitleImage != null)
        {
            _titleImage.Texture = TitleImage;
        }
        else
        {
            _titleImage.QueueFree();
        }

        if (BackgroundVideo != null)
        {
            _backgroundVideo.Stream = BackgroundVideo;
            _backgroundVideo.Play();
        }
        else
        {
            _backgroundVideo.QueueFree();
        }

        if (MusicPlaylist != null && MusicPlaylist.Count > 0 && MusicPlaylist[_playlistIndex] != null)
        {
            PlayTrack(_playlistIndex);
        }

        if (string.IsNullOrEmpty(CreditsFile))
        {
            GD.PushError("At least one credits file must be provided.");
            Debug.Assert(false);
        }

        if (!FileAccess.FileExists(CreditsFile))
        {
            GD.PushError("Credits file does not exist.");
            Debug.Assert(false);
        }

        _file = FileAccess.Open(CreditsFile, FileAccess.ModeFlags.Read);
        _credits = _file.GetAsText();
        _file.Close();

        ParseCreditsFile();
    }

    public override void _Process(double delta)
    {
        if (_isFirstFrame)
        {
            _viewSize = GetViewport().GetVisibleRect().Size;
            _scrollingContainer.Position = new Vector2(_scrollingContainer.Position.X, _viewSize.Y);
            _isFirstFrame = false;
        }

        if (!_done)
        {
            if (_scrollingContainer.Position.Y + _scrollingContainer.Size.Y > 0)
            {
                _scrollingContainer.Position = new Vector2(_scrollingContainer.Position.X, (float)(_scrollingContainer.Position.Y - Speed * delta));
            }
            else
            {
                End();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_done)
        {
            if (EnableControls)
            {
                if (@event.IsActionPressed(SlowDownControl))
                {
                    Speed -= 10 * (int)@event.GetActionStrength(SlowDownControl);
                }
                if (@event.IsActionPressed(SpeedUpControl))
                {
                    Speed += 10 * (int)@event.GetActionStrength(SpeedUpControl);
                }
            }

            if (EnableSkip)
            {
                if (@event.IsActionPressed(SkipControl))
                {
                    End();
                }
            }
        }
    }

    private void ParseCreditsFile()
    {
        scrolling_text scrollingText = null;
        Label centeredText = null;
        var title = new Label();
        var name = new Label();

        var lines = _credits.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("{") && trimmedLine.EndsWith("}"))
            {
                scrollingText = null;
                centeredText = new Label();

                centeredText.HorizontalAlignment = HorizontalAlignment.Center;
                centeredText.VerticalAlignment = VerticalAlignment.Center;

                centeredText.AddThemeColorOverride("font_color", NameColor);
                centeredText.AddThemeFontSizeOverride("font_size", 26);
                if (CustomFont != null)
                {
                    title.AddThemeFontOverride("font", CustomFont);
                }

                trimmedLine = trimmedLine.Replace("{", "").Replace("}", "");
                centeredText.Text += trimmedLine;

                _scrollingContainer.AddChild(centeredText);
            }
            else
            {
                if (scrollingText == null)
                {
                    scrollingText = GD.Load<PackedScene>("res://credits-scene/scrollingText.tscn").Instantiate<scrolling_text>();
                    _scrollingContainer.AddChild(scrollingText);

                    title = scrollingText.Title;
                    name = scrollingText.Name;

                    title.AddThemeColorOverride("font_color", TitleColor);
                    name.AddThemeColorOverride("font_color", NameColor);
                    title.AddThemeFontSizeOverride("font_size", 26);
                    name.AddThemeFontSizeOverride("font_size", 26);

                    if (CustomFont != null)
                    {
                        title.AddThemeFontOverride("font", CustomFont);
                        name.AddThemeFontOverride("font", CustomFont);
                    }

                    scrollingText.GetNode<MarginContainer>("margin").AddThemeConstantOverride("margin_right", Margin / 2);
                    scrollingText.GetNode<MarginContainer>("margin").AddThemeConstantOverride("margin_left", Margin / 2);
                }

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    title.Text += "\n";
                    name.Text += "\n";

                    if (i > 0 && (lines[i - 1].StartsWith("[") && lines[i - 1].EndsWith("]")))
                    {
                        title.Text += "\n";
                        name.Text += "\n";
                    }
                }
                else if (trimmedLine.StartsWith("[") && (trimmedLine.EndsWith("]")))
                {
                    if (i > 0 && (lines[i - 1].StartsWith("[") && lines[i - 1].EndsWith("]")))
                    {
                        title.Text += "\n";
                        name.Text += "\n";
                    }

                    trimmedLine = trimmedLine.Replace("[", "").Replace("]", "");
                    title.Text += trimmedLine;
                }
                else
                {
                    name.Text += trimmedLine + "\n";
                    title.Text += "\n";
                }
            }
        }
    }

    public void OnBackgroundVideoFinished()
    {
        if (LoopVideo)
        {
            _backgroundVideo.Play();
        }
    }

    private void PlayTrack(int index)
    {
        if (0 <= index && index < MusicPlaylist.Count)
        {
            MusicPlaylist[index].Set("loop", false);
            _musicPlayer.Stream = MusicPlaylist[index];
            _musicPlayer.Play();
            _playlistIndex = index;
        }
    }

    public void OnMusicPlayerFinished()
    {
        if (_playlistIndex + 1 < MusicPlaylist.Count)
        {
            PlayTrack(_playlistIndex + 1);
        }
        else if (LoopPlaylist)
        {
            _playlistIndex = 0;
            PlayTrack(_playlistIndex);
        }
    }

    private void End()
    {
        EmitSignal("Ended");
        _done = true;

        if (NextScene != null)
        {
            GetTree().ChangeSceneToFile(NextScene.ResourcePath);
        }
        else if (QuitOnEnd)
        {
            GetTree().Quit();
        }
        else if (DestroyOnEnd)
        {
            this.QueueFree();
        }
    }
}