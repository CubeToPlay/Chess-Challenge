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
            var transposition_table = new Dictionary<ulong, (double, double, double, double)>();

            double[]
            score_weights = { 2, 0, 1 }, /* piece count, board control, board control(singular) */
            piece_values = { 0, 1, 4, 5, 5, 9, 10 }; // null, pawn, knight, bishop, rook, queen, king

            int evaluations = 0;

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
                    int persepctive = (piece_is_white == board.IsWhiteToMove) ? 1 : -1;

                    score += piece_values[(int)current_piece_type] * piece_list.Count * persepctive * score_weights[0];
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

                        for (int i = 0; i < 64; i++) values[i] += Convert.ToByte(((long)current_bit_board & ((long)1 << i)) != 0) * persepctive;

                    }
                }

                foreach (double value in values) score += value * score_weights[1] + Math.Clamp(value, -1, 1) * score_weights[2];

                return score;
            }

            double board_state()
            {
                return 100 * Convert.ToInt64(board.IsInCheckmate()) - 150 * Convert.ToInt64(board.IsDraw()) /*- 50 * Convert.ToInt64(board.IsRepeatedPosition())*/;
            }

            Move[] ordered_moves(bool captures_only)
            {
                var move_dictionary = board.GetLegalMoves(captures_only).ToDictionary(move => move, score => new double());

                foreach (Move move in move_dictionary.Keys)
                {
                    double
                        piece_value = piece_values[(int)move.MovePieceType],
                        capture_value = piece_values[(int)move.CapturePieceType];

                    move_dictionary[move] =
                        (board.SquareIsAttackedByOpponent(move.StartSquare) ? piece_value * 2 : 0) +
                        (move.IsPromotion ? 10 : 0) +
                        (board.SquareIsAttackedByOpponent(move.TargetSquare) ? (capture_value - piece_value) * 10 : 0)
                        ;

                    //move_dictionary[move] = 0;
                }


                return move_dictionary.OrderByDescending(move => move.Value).ToDictionary(move => move.Key, move => move.Value).Keys.ToArray();
            }

            double minimax(ref Move best_move, int max_depth, int depth = 0, double players_best_score = double.MinValue, double opponents_worst_score = double.MaxValue, bool extend = false)
            {
                evaluations++;

                var zobrist_key = board.ZobristKey;

                if (!transposition_table.ContainsKey(zobrist_key)) transposition_table.Add(zobrist_key, (board_score(), double.MinValue, double.MinValue, double.MaxValue));

                double current_board_score = transposition_table[zobrist_key].Item1 - board_state();

                // players_best_score = transposition_table[zobrist_key].Item3 == double.MinValue ? players_best_score : transposition_table[zobrist_key].Item3;
                //opponents_worst_score = transposition_table[zobrist_key].Item4 == double.MaxValue ? opponents_worst_score : transposition_table[zobrist_key].Item4;

                if (extend)
                {
                    if (current_board_score > opponents_worst_score) return opponents_worst_score; // This move is too strong, opponent won't pick it

                    players_best_score = Math.Max(current_board_score, players_best_score);
                }

                var moves = ordered_moves(false);

                if (moves.Length == 0) return current_board_score;

                if (depth == max_depth) return minimax(ref best_move, max_depth, max_depth - 1, players_best_score, opponents_worst_score, true);

                //if (transposition_table[zobrist_key] != double.MinValue) return transposition_table[zobrist_key];

                depth++;

                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    if (extend && move.CapturePieceType == PieceType.None && !board.IsInCheck() || board.IsDraw() && moves.Length > 1)
                    {
                        board.UndoMove(move);
                        continue;
                    }

                    double players_best_choice = -minimax(ref best_move, max_depth, depth, -opponents_worst_score, -players_best_score, extend);
                    board.UndoMove(move);

                    if (players_best_score < players_best_choice && depth == 1) best_move = move;

                    if (players_best_choice >= opponents_worst_score) return opponents_worst_score; // Opponent won't choose this choice

                    players_best_score = Math.Max(players_best_score, players_best_choice);

                    //Console.WriteLine(opponent_best_option + " " + best_option);

                    /*if (maximizer ? best_score > beta : best_score < alpha) break;

                    alpha = maximizer ? Math.Max(alpha, best_score) : alpha;
                    beta = !maximizer ? Math.Min(beta, best_score) : beta;*/
                }

                //Console.WriteLine(zobrist_values.Count + " " + zobrist_index);
                transposition_table[zobrist_key] = (transposition_table[zobrist_key].Item1, players_best_score, players_best_score, opponents_worst_score);

                return players_best_score;
            }


            Move next_move = Move.NullMove;
            double best_score = double.MinValue;
            //next_move = board.GetLegalMoves()[0];

            //minimax(ref next_move, 3);

            for (int i = 3; i <= 3; i += 2)
            {
                Move current_move = Move.NullMove;
                double current_score = minimax(ref current_move, i);

                if (current_score > best_score)
                {
                    best_score = current_score;
                    next_move = current_move;
                }
            }

            //Console.WriteLine(best_score + " " + evaluations);

            //Console.WriteLine(board_score());

            if (next_move == Move.NullMove) Console.WriteLine("Resign");

            return next_move;
        }
    }
}