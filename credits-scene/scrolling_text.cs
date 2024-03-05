using Godot;

public partial class scrolling_text : HBoxContainer
{
    public Label Title { get; set; }
    public Label Name { get; set; }

    public override void _Ready()
    {
        Title = GetNode<Label>("margin/Titles");
        Name = GetNode<Label>("margin2/Names");
    }
}