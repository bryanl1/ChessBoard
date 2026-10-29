using Avalonia.Controls;
using Avalonia.Data;
using Cecs475.BoardGames.Chess.AvaloniaView;
using MsBox.Avalonia;

namespace Cecs475.BoardGames.AvaloniaApp;

public partial class MainView : UserControl
{
	public ChessView ViewControl {
		get => (ChessView)this.FindResource("view")!;
	}
	public MainView() {
		InitializeComponent(); 
		this.Resources.Add("vm", ViewControl.ChessViewModel);
		ViewControl.ChessViewModel.GameFinished += ViewModel_GameFinished;
	}

	private void ViewModel_GameFinished(object? sender, System.EventArgs e) {
		var message = MessageBoxManager.GetMessageBoxStandard("Game over, man!", "Game over!");
		message.ShowAsPopupAsync(this);
	}

	private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
		ViewControl.ChessViewModel.UndoMove();
	}
}
