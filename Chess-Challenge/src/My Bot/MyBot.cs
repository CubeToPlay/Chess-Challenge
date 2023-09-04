using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        ulong[] bitboards = {103481868288, 66125924401152}; // Center, Center Outline
        float[,] piece_scores = { { 0, 0.7f, 3.1f, 3.3f, 5.0f, 7.9f, 2.2f}, {0, 1.3f, 3.1f, 3.3f, 5.0f, 7.9f, 2.2f}}; // None, Pawn, Knight, Bishop, Rook, Queen, King
        float[] weights = { 3f, 0.5f, 0f,  1f, 0f}; // Piece Count, Board Control, Center Control, King Danger, Center Outline
        //BitboardHelper.VisualizeBitboard(bitboards[1]);

        var transposition_table = new Dictionary<ulong, (float, Move[])>();

        Move best_move = Move.NullMove;
        int evaluations = 0;

        float board_score(int depth = 1, bool debug = false)
        { 
            float score = 0;

            ulong current_bitboard = board.IsWhiteToMove? board.WhitePiecesBitboard : board.BlackPiecesBitboard;

            for (int i = 0; i < 64; i++)
            {
                Piece current_piece = board.GetPiece(new Square(i));
                if (current_piece.IsKing) continue;
                ulong piece_bitboard = BitboardHelper.GetPieceAttacks(current_piece.PieceType, current_piece.Square, board, current_piece.IsWhite);
                float perspective = (current_piece.IsWhite != board.IsWhiteToMove) ? 1 : -1;


                score += 
                    BitboardHelper.GetNumberOfSetBits(piece_bitboard) * weights[1] * perspective /*+
                    BitboardHelper.GetNumberOfSetBits(piece_bitboard & bitboards[0]) * weights[2] * perspective +
                    BitboardHelper.GetNumberOfSetBits(piece_bitboard & bitboards[1]) * weights[4] * perspective*/;
                    

                current_bitboard |= piece_bitboard;

                score += piece_scores[0, (int)current_piece.PieceType] * perspective * weights[0];
            }

            //score -= BitboardHelper.GetNumberOfSetBits(current_bitboard & BitboardHelper.GetKingAttacks(board.GetKingSquare(board.IsWhiteToMove))) * weights[3];

            score += (Convert.ToInt32(board.IsInCheckmate()) * 100f - Convert.ToInt32(board.IsDraw()) * 50f)/* - depth*/;

            return -score;
        }

        int same_move_value = 0;
        ulong same_move_bitboard = 0;

        bool checkmate_found = false;
        int checkmate_depth = int.MaxValue;

        float minimax(int search_depth, int depth = 0, float alpha = float.MinValue, float beta = float.MaxValue, bool extend = false)
        {
            evaluations++;

            if (!transposition_table.ContainsKey(board.ZobristKey)) transposition_table[board.ZobristKey] = (board_score(depth), board.GetLegalMoves());

            var current_table = transposition_table[board.ZobristKey];

            float evaluation = current_table.Item1;
            var moves = current_table.Item2;

            if (extend)
            {
                alpha = Math.Max(alpha, evaluation);
                if (evaluation >= beta) return beta;
            }

            //if (evaluation > 20) search_depth = 5;

            if (depth == search_depth) return minimax(search_depth, depth + 1, alpha, beta, true); // Extended Minimax

            //var indexes = order_moves(moves);

            /*foreach (int index in indexes)
            {
                Move move = moves[index];*/

            foreach (Move move in moves)
            { 
                if (extend && (!move.IsCapture || depth == checkmate_depth || depth == 11 || timer.MillisecondsRemaining <= 20000) && !board.IsInCheck()) continue;

                board.MakeMove(move);
                if (board.IsInCheckmate() && depth % 2 == 1)
                {
                    checkmate_depth = Math.Min(checkmate_depth, depth);
                    checkmate_found = true;
                }
                evaluation = -minimax(search_depth, depth + 1, -beta, -alpha, extend); // Switch Sides
                board.UndoMove(move);

                if (alpha >= beta) return beta;

                if (alpha < evaluation && depth == 0)
                {
                    same_move_bitboard = 0;
                    same_move_value = 0;
                    best_move = move;
                }


                alpha = Math.Max(evaluation, alpha);

                if (alpha == evaluation && depth == 0)
                {
                    BitboardHelper.SetSquare(ref same_move_bitboard, move.StartSquare);
                    same_move_value++;
                }
            }

            return alpha;
        }

        Console.WriteLine(minimax(3) + " " + evaluations + " " + board_score() + " " + same_move_value + " " + checkmate_found + " " + checkmate_depth);
        //BitboardHelper.VisualizeBitboard(same_move_bitboard);

        return best_move;
    }
}