# AI Usage Journal
**Course:** [CECS 475]
**Assignment:** [Project 2]
**Student:** [Bryan Linares]
**Date:** [2025-12-13]
---
## 1. Tools Used
List any AI tools you used (by name and version if known).
- [X] AI tools were used (listed below):
**Tool(s):**
ChatGPT (GPT-5, November 2025), 
GitHub Copilot (VSCode extension) - using ChatGPT5 mini


## 2. Prompts / Queries
Identify the *exact* prompts or queries you entered into the AI, and what you did
with them.
If too long, include representative excerpts. For each prompt, record:
### a. Prompt text
**Example:**
> "Generate the 6 chess pieces each for black and white, in a hand drawn sketch style, with each file named [color]-[piece].png"
> "Explain how to make a converter to convert, checking the downcasts, for loading an Image from an Avalonia resource"
> "Help with a sekelton of the multiconverter, for the grid that binds to the ChessSquare collection of the ViewModel and generates the 64 squares. Make each square a single solid color with no piece image to start."

> "Need help loading images again for the pawnpromotion window. You may add new properties or methods to the ViewModel."

> "Help with checking for the King being in check for the turn player, and highlighting in yellow."

> Various other smaller prompts, like there were also some trial and error prompts like "The promotion window is all White", with fix suggestions.

### b. Raw AI Output(s)
Copy the AI’s unedited response(s) (or excerpts).
If too long, provide the most relevant section(s).
### c. What I Accepted, Modified, or Rejected

Briefly explain what you did with the AI output of this prompt.
- **Accepted:** (e.g., “Used AI’s idea for a Card struct design.”)
"Used this general pattern for receiving and downcasting, checking types"
```csharp
            if (value is ChessPiece d) piece = d;
            if (piece == null) return null;

            if (!(piece is ChessPiece pnn)) return null;
            var pType = pnn.PieceType;
            if (pType == ChessPieceType.Empty) return null;

            string role = pType.ToString().ToLowerInvariant();//lowercase function..
            string color = pnn.Player == 1 ? "white" : "black";

            var candidates = new List<string> {
                $"{color}-{role}.png"
            };
```

"For the multiconverter, the AI quickly generated safe robust null checking and downcasts for the input"
```csharp
				var rowObj = values[0];
				var colObj = values[1];
				object? selObj = values.Count > 2 ? values[2] : null;
				object? hovObj = values.Count > 3 ? values[3] : null;
				object? hlObj = values.Count > 4 ? values[4] : null;
				object? checkObj = values.Count > 5 ? values[5] : null;
				int rowInt = rowObj is int r ? r : System.Convert.ToInt32(rowObj);
				int colInt = colObj is int c ? c : System.Convert.ToInt32(colObj);
```

"Using a direct binding to Row and Col and exposing them in the ChessSquare was used, it turned out to be more useful later when having to check for King in Check"

"This RebindState function is based off the OthelloView version, but addition  of king checking logic to it here is a interesting decision from the AI"
```csharp
		private void RebindState() {
			var positions = BoardPosition.GetRectangularPositions(8, 8).ToList();
			for (int i = 0; i < positions.Count; ++i) {
				var pos = positions[i];
				mSquares[i].Piece = mBoard.GetPieceAtPosition(pos);
				mSquares[i].IsSelected = false;
				mSquares[i].IsHovered = false;
				mSquares[i].IsHighlighted = false;
				// mark king when in check
				var sqPiece = mBoard.GetPieceAtPosition(pos);
				mSquares[i].IsKingInCheck = (mBoard.IsCheck && sqPiece.PieceType == ChessPieceType.King && sqPiece.Player == mBoard.CurrentPlayer);
			}

			OnPropertyChanged(nameof(BoardAdvantage));
			OnPropertyChanged(nameof(CurrentPlayer));
			OnPropertyChanged(nameof(CanUndo));
		}
```

"In promotionwindow.axaml.cs the AI made the repeatable pattern for loading the piece images again"
```csharp
            var imgKnight = this.FindControl<Image>("imgKnight");

            if (imgKnight != null)
            {
                using var s = AssetLoader.Open(new Uri(basePath + $"{color}-knight.png"));
                imgKnight.Source = new Bitmap(s);
            }
```

"For King checking, another bool was exposed. Since it would have to affect the square coloring at any time"
```XML
    <Binding Path="IsKingInCheck" />
```
```csharp
				mSquares[i].IsKingInCheck = (mBoard.IsCheck && sqPiece.PieceType == ChessPieceType.King && sqPiece.Player == mBoard.CurrentPlayer);

```

"In chessview.axaml.cs This code was generated to properly parent the popup promotion window. I assume this is ok way to do it"
```csharp

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var dlg = new PromotionWindow(vm, start, end);
        if (owner != null) {
            await dlg.ShowDialog((Window)owner);
        } else {
            await dlg.ShowDialog((Window?)null!);
        }
        return;
```

- **Modified:** (e.g., “Changed AI’s Card class to use auto properties.”)
- **Rejected:** (e.g., “Ignored AI’s suggestion for a parameter-less constructor.
    -All the PNG made
    -A few bad versions of code generated. 


## 3. Reflection
1–3 sentences about how the AI helped or hindered your work.
**Example:**
>  The output chess png files were too big and had issues with sizing and transparency. I abandoned this idea, for PNGs I found online. 
> For the converters, the AI was generally pretty good about doing as much possible null checking and making sure the code had fallbacks in cases of failure.
> In the multiconverter, after seeing the pattern of what is necessary, exposing a variable, binding it, and making sure the code works with it correctly is a straightforward pattern
> There are a few points where the AI has to guess what the best approach was, and went down the wrong path. So starting the code off with booleans and functions I knew from the Model made it work better. For example, just letting it know the enums that the PieceType were using in the Model made the code much simpler instead of guesswork. 
> The king in check logic is interesting to make more efficient, and the decision of putting it in the Rebind state is something I might think about in the future, when thinking about efficieny. But I let the AI decide here. The RebindState function in the Othello work uses a HashSet which is better here too. Something to do in the future. 
> Overall I would say the AI was helpful but would go down very bad paths trying to make things "robust" without enough context, like getting very greedy with the image loading logic and fallbacks, but the code was pretty simple, since the CHessPiece object contained all the info needed. Something like the UndoMove functionality works simply, because it all comes for free from the Model, so it was really about binding the right value and updating the bindings and visuals where appropriate.