using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    Random random;

    static int[] PIECE_SCORES = { 0, 70, 310, 330, 500, 790, 0 };
    static int PIECE_SCORES_TOTAL = PIECE_SCORES.Sum();
    static int SEARCH_DEPTH = 2;
    static int CHECKMATE_SCORE = 10000;

    static Dictionary<string, KeyValuePair<int, Move[]>> MoveTranspositionTable = new Dictionary<string, KeyValuePair<int, Move[]>>();
    static Dictionary<ulong, int> ScoreTranspositionTable = new Dictionary<ulong, int>();

    int entries = 0;

    private int boardState(Board board, bool isWhite = false, int depth = 0)
    {
        bool isMyTurn = board.IsWhiteToMove == isWhite;


        double time = Math.Pow((depth + 1) / SEARCH_DEPTH, 0.5);

        return (isMyTurn ? -1 : 1) * ((board.IsInCheckmate() ? (int)(CHECKMATE_SCORE * time) : 0) - (board.IsDraw() ? 900 : 0) - (board.IsRepeatedPosition() ? 200 : 0));
    }
    private int boardScore(Board board, bool isWhite, bool debug = false)
    {
        bool isMyTurn = board.IsWhiteToMove == isWhite;

        if (!ScoreTranspositionTable.ContainsKey(board.ZobristKey))
        {
            entries++;
            float score = 0;

            var bitPieceControl = new int[2] { 0, 0 };
            var bitPieceBoardControl = new ulong[2] { isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard, isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard};
            var bitPieceTypeControl = new Dictionary<int, ulong> { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 } };
            var bitBoardControl = new ulong[2] { 0, 0 };

            ulong pieceLocationBitboard = 0;

            foreach (var pieceList in board.GetAllPieceLists())
            {
                bool isMyPiece = pieceList.IsWhitePieceList == isWhite;
                foreach (var piece in pieceList)
                {
                    pieceLocationBitboard = 0;

                    bitPieceControl[isMyPiece ? 0 : 1] += PIECE_SCORES[(int)piece.PieceType]; // Gets the total piece control for each side
                    bitBoardControl[isMyPiece ? 0 : 1] |= BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite); // Gets the total board control for each side
                    
                    pieceLocationBitboard = bitPieceTypeControl[(int)piece.PieceType];
                    BitboardHelper.SetSquare(ref pieceLocationBitboard, piece.Square);
                    bitPieceTypeControl[(int)piece.PieceType] |= pieceLocationBitboard;
                }
            }

            score += bitPieceControl[0] - bitPieceControl[1]; // Score Difference
            score += BitboardHelper.GetNumberOfSetBits(bitBoardControl[0] ^ (bitPieceBoardControl[1] & bitPieceBoardControl[0])) / 2; // Spaces controlled
            score += BitboardHelper.GetNumberOfSetBits(bitBoardControl[0] & bitPieceBoardControl[0]) * 2; // Pieces that you control
            score += BitboardHelper.GetNumberOfSetBits(bitBoardControl[0] & (bitPieceBoardControl[1] ^ (bitBoardControl[1] & bitPieceBoardControl[1]))); // Gets their pieces that you have control of that they are not controlling
            score -= BitboardHelper.GetNumberOfSetBits(bitBoardControl[1] & (bitPieceBoardControl[0] ^ (bitBoardControl[0] & bitPieceBoardControl[0]))) / 2; // Gets your pieces that they have control of that you are not controlling
            //score += BitboardHelper.GetNumberOfSetBits(bitBoardControl[0] & bitPieceBoardControl[1]); // Gets their pieces that you have control of
            //score += BitboardHelper.GetNumberOfSetBits(bitBoardControl[0] & (bitPieceBoardControl[1] | bitPieceBoardControl[0])); // Pieces that you control
            //score += BitboardHelper.GetNumberOfSetBits(~bitPieceBoardControl[0] & (bitBoardControl[0] ^ (bitBoardControl[1] & bitBoardControl[0]))); // Gets the squares that you have complete control of

            foreach ((int pieceType, ulong pieceTypeControl) in bitPieceTypeControl)
            {
                //score -= pieceType * BitboardHelper.GetNumberOfSetBits(bitBoardControl[1] & bitPieceBoardControl[0]); // Gets your pieces that they have control of
                //score -= pieceType * BitboardHelper.GetNumberOfSetBits(pieceTypeControl & (bitBoardControl[0] ^ (bitBoardControl[0] | bitPieceBoardControl[0]))); // Gets your pieces that you don't have control of
                //score -= pieceType * BitboardHelper.GetNumberOfSetBits(pieceTypeControl & (bitBoardControl[1] & (bitPieceBoardControl[0] ^ (bitBoardControl[0] & bitPieceBoardControl[0])))); // Gets your pieces that they have control of that you are not controlling
                //score += pieceType * BitboardHelper.GetNumberOfSetBits(pieceTypeControl & (bitBoardControl[0] & (bitPieceBoardControl[1] | bitPieceBoardControl[0]))); // Pieces that you control
            }


            score -= (isMyTurn ? 1 : -1) * (board.IsInCheck() ? 10 : 0);

            if (debug)
            {
                //Console.WriteLine("0: " + bitPieceControl[0].ToString() + " 1: " + bitPieceControl[1].ToString() + " D: " + (bitPieceControl[0] - bitPieceControl[1]).ToString());
                Console.WriteLine("score: " + score.ToString());
                //BitboardHelper.VisualizeBitboard(bitBoardControl[0] & (bitPieceBoardControl[1] ^ (bitBoardControl[1] & bitPieceBoardControl[1])));
                BitboardHelper.VisualizeBitboard(bitBoardControl[1] & (bitPieceBoardControl[0] ^ (bitBoardControl[0] & bitPieceBoardControl[0])));
            }

            ScoreTranspositionTable.Add(board.ZobristKey, (int)score);
        }

        return ScoreTranspositionTable[board.ZobristKey];
    }

    private Move[] getOrderedMoves(Board board, bool catpuresOnly = false)
    {
        var moveOrderScore = new Dictionary<Move, int>();
        foreach (Move move in board.GetLegalMoves(catpuresOnly))
        {
            int score = 0;

            score += PIECE_SCORES[(int)move.CapturePieceType];
            score += PIECE_SCORES[(int)move.MovePieceType] * (move.IsCapture ? 1 : 0);
            score += PIECE_SCORES[(int)move.MovePieceType];

            //score -= BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove));

            moveOrderScore.Add(move, score);
        }

        return (from entry in moveOrderScore orderby entry.Value descending select entry.Key).ToArray();
    }

    private KeyValuePair<int, Move[]> Evaluate(Board board, Timer timer, int depth, bool isWhite, bool capturesOnly = false, int alpha = int.MinValue, int beta = int.MaxValue)
    {
        if (MoveTranspositionTable.ContainsKey(board.GetFenString())) { return MoveTranspositionTable[board.GetFenString()]; }

        bool myTurn = board.IsWhiteToMove == isWhite;

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) { 
            return new KeyValuePair<int, Move[]>( (myTurn ? 1 : -1) * (boardScore(board, board.IsWhiteToMove) + boardState(board, board.IsWhiteToMove, depth)), Array.Empty<Move>()); 
        }
        
        if (myTurn) { depth--; }
        var best = new KeyValuePair<int, Move[]>( myTurn ? int.MinValue : int.MaxValue, Array.Empty<Move>());

        foreach (Move move in getOrderedMoves(board, capturesOnly))
        {
            board.MakeMove(move);
            var result = Evaluate(board, timer, depth, isWhite, capturesOnly, alpha, beta);
            /*if (MoveTranspositionTable.ContainsKey(board.GetFenString())) { result = MoveTranspositionTable[board.GetFenString()]; }
            else { result = Evaluate(board, timer, depth, isWhite, capturesOnly, alpha, beta); }*/
            MoveTranspositionTable.TryAdd(board.GetFenString(), result);
            var evaluation = result.Key;
            board.UndoMove(move);



            if (best.Key == evaluation) { best = new KeyValuePair<int, Move[]>(evaluation, best.Value.Append(move).ToArray()); }
            if (myTurn)
            {
                if (best.Key < evaluation) { best = new KeyValuePair<int, Move[]>(evaluation, new Move[] { move }); }
                alpha = Math.Max(best.Key, alpha);
            }
            else
            {
                if (best.Key > evaluation) { best = new KeyValuePair<int, Move[]>(evaluation, new Move[] { move }); }
                beta = Math.Min(best.Key, beta);
            }

            if (beta <= alpha) { break; }
        }

        return best;
    }
    public Move Think(Board board, Timer timer)
    {
        MoveTranspositionTable.Clear();

        entries = 0;
        ulong debugBitboard = 0;
        Move nextMove = Move.NullMove;

        var result = Evaluate(board, timer, SEARCH_DEPTH, board.IsWhiteToMove);
        //MoveTranspositionTable.TryAdd(board.GetFenString(), result);

        //Console.WriteLine(result.Value.Length);
        Console.WriteLine(result.Key);
        //Console.WriteLine(entries);
        //Console.WriteLine(transpositionTable.Count());
        //Console.WriteLine(ScoreTranspositionTable.Count());
        foreach (Move move in result.Value)
        { BitboardHelper.SetSquare(ref debugBitboard, move.StartSquare); }


        if (nextMove == Move.NullMove)
        { nextMove = result.Value[0]; }


        

        //BitboardHelper.VisualizeBitboard(debugBitboard);

        board.MakeMove(nextMove);
        boardScore(board, !board.IsWhiteToMove, true);
        board.UndoMove(nextMove);


        return nextMove;
    }
}