using ChessChallenge.API;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Xml.Schema;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int search_depth = 2;

        // Piece values:       null, pawn, knight, bishop, rook, queen, king
        int[] piece_values = { 0, 1, 4, 5, 5, 9, 10 };

        double get_score(Board input_board)
        {
            double board_score = 0;

            foreach (PieceList piece_list in input_board.GetAllPieceLists()) board_score += piece_values[(int)piece_list.TypeOfPieceInList] * piece_list.Count * (piece_list.IsWhitePieceList ? 1 : -1); 

            return board_score * (input_board.IsWhiteToMove ? -1 : 1);
        }

        double minimax(int depth, int index, bool maximizer, int max_depth, Board input_board)
        {
            Console.WriteLine(depth + " " + index);
            if (depth == max_depth) return get_score(input_board); 

            depth += 1;

            if (maximizer) 
                return Math.Max(minimax(depth, index, false, max_depth, input_board), minimax(depth, index, false, max_depth, input_board));
            else 
                return Math.Min(minimax(depth, index, true, max_depth, input_board), minimax(depth, index, true, max_depth, input_board));
        }

        Console.WriteLine(minimax(0, 0, true, search_depth, board));

        return board.GetLegalMoves()[0];
    }
}