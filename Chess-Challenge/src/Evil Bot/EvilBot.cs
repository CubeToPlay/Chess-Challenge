using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        public Move Think(Board board, Timer timer)
        {
            double[]
        score_weights = { 1, 1, 0 }, /* piece count, board control, board control(singular) */
        piece_values = { 0, 1, 4, 5, 5, 9, 10 }; // null, pawn, knight, bishop, rook, queen, king

            /* int bool_to_int(bool boolean)
             { return boolean ? 1 : 0; }*/

            double board_score()
            {
                var values = new double[64];

                double score = 0;

                foreach (PieceList piece_list in board.GetAllPieceLists())
                {
                    PieceType current_piece_type = piece_list.TypeOfPieceInList;
                    bool piece_is_white = piece_list.IsWhitePieceList;
                    int sign = (piece_is_white == board.IsWhiteToMove) ? 1 : -1;

                    score += piece_values[(int)current_piece_type] * piece_list.Count * sign * score_weights[0];
                    foreach (Piece piece in piece_list)
                    {
                        Square current_piece_square = piece.Square;
                        ulong current_bit_board = 0;

                        switch (current_piece_type)
                        {
                            case PieceType.Pawn:
                                current_bit_board = BitboardHelper.GetPawnAttacks(current_piece_square, piece_is_white);
                                break;
                            case PieceType.Knight:
                                current_bit_board = BitboardHelper.GetKnightAttacks(current_piece_square);
                                break;
                            case PieceType.King:
                                current_bit_board = BitboardHelper.GetKingAttacks(current_piece_square);
                                break;
                            case PieceType.Bishop:
                            case PieceType.Rook:
                            case PieceType.Queen:
                                current_bit_board = BitboardHelper.GetSliderAttacks(current_piece_type, current_piece_square, board.AllPiecesBitboard);
                                break;
                        }

                        for (int i = 0; i < 64; i++) values[i] += Convert.ToByte(((long)current_bit_board & ((long)1 << i)) != 0) * sign;

                    }
                }

                foreach (double value in values) score += value * score_weights[1] + Math.Clamp(value, -1, 1) * score_weights[2];



                score -= board.IsInCheckmate() ? 100 : (board.IsDraw() ? -100 : (board.IsRepeatedPosition() ? -30 : 0));

                return score;
            }

            List<double> ordered_moves(bool captures_only)
            {
                var moves = board.GetLegalMoves(captures_only);
                var scores = new double[moves.Length].ToList();

                int index = 0;

                foreach (Move move in moves)
                {
                    scores[index] =
                        piece_values[(int)move.CapturePieceType] +
                        piece_values[(int)move.MovePieceType] +
                        (board.SquareIsAttackedByOpponent(move.StartSquare) ? 20 : 0) +
                        (move.IsPromotion ? 10 : 0) +
                        (board.SquareIsAttackedByOpponent(move.StartSquare) ? -10 : 0) +
                        (board.IsInCheck() ? 5 : 0)
                        ;

                    index++;
                }

                return scores;
            }

            double minimax(int depth = 5, double alpha = double.MinValue, double beta = double.MaxValue)
            {
                //max_depth = (timer.MillisecondsRemaining < 30000) ? 4 : 2;

                double score = board_score();
                var board_moves = board.GetLegalMoves().ToList();
                //Console.WriteLine(captures_only + " " + depth + " " + board_moves.Count);

                if (depth == 0 || board_moves.Count == 0) return  board_score();



                var move_scores = ordered_moves(false);

                depth--;

                while (move_scores.Count > 0)
                {
                    int best_move_index = move_scores.IndexOf(move_scores.Max());
                    Move move = board_moves[best_move_index];

                    move_scores.RemoveAt(best_move_index);
                    board_moves.RemoveAt(best_move_index);

                    board.MakeMove(move);
                    //depth += Convert.ToByte(/*board.IsInCheck() || */board.SquareIsAttackedByOpponent(move.TargetSquare)) ;



                    //depth = Math.Min(depth, 3);
                    score = -minimax(depth, -beta, -alpha);
                    board.UndoMove(move);

                    if (score >= beta) return beta;

                    alpha = Math.Max(alpha, score);

                    //Console.WriteLine(depth - 1 + " " + -beta + " " + -alpha + " " + score);
                }

                return alpha;
            }

            Move next_move = board.GetLegalMoves()[0];

            double worst = double.MaxValue;
            /*for (int i = 2; i <= 4; i += 2)
            {*/
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);

                double score = minimax(2, double.MinValue, worst);

                if (worst > score) next_move = move;

                worst = Math.Min(worst, score);

                //Console.WriteLine(worst);

                board.UndoMove(move);


            }
            //}

            

            

            return next_move;
        }
    }
}