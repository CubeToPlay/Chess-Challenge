using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        double[] 
        score_weights = { 2, 0, 1},
        piece_values = { 0, 1, 4, 5, 5, 9, 10 }; // Piece values: null, pawn, knight, bishop, rook, queen, king


        //int entries = 0;

        
        List<double> rank_moves(List<Move> moves)
        {
            var move_scores = new double[moves.Count];

            int index = 0;

            foreach (Move move in moves)
            {
                double
                move_piece_value = piece_values[(int)move.MovePieceType],
                capture_piece_value = piece_values[(int)move.CapturePieceType];
                double input_board_values = board_score();

                /*board.MakeMove(move);
                move_scores[index] += input_board_values - board_score();
                move_scores[index] += board.IsInCheckmate() ? 100 : 0;
                board.UndoMove(move);*/

                move_scores[index] += capture_piece_value;
                move_scores[index] += board.SquareIsAttackedByOpponent(move.TargetSquare) ? capture_piece_value - move_piece_value : capture_piece_value;
                move_scores[index] += board.SquareIsAttackedByOpponent(move.StartSquare) ? move_piece_value : 0;
                index++;
            }

            return move_scores.ToList();
        }
        

        double board_score()
        {
            double[] values = new double[64];

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

                    for (int i = 0; i < 64; i++) values[i] += (((long)current_bit_board & ((long)1 << i)) != 0 ? 1 : 0) * sign;

                }
            }

            foreach (double value in values) score += value * score_weights[1] + Math.Clamp(value, -1, 1) * score_weights[2]; 



            score += board.IsInCheckmate() ? 100 : (board.IsDraw() ? -50 : 0);

            return score;
        }


        double minimax(ref Move best_move, int depth, int max_depth, bool maximizer, double alpha = double.MinValue, double beta = double.MaxValue)
        {
            //entries++;

            if (depth == 0 || board.IsInCheckmate() || board.IsDraw()) return board_score();

            depth--;

            double best = maximizer ? double.MinValue : double.MaxValue;

            var moves = board.GetLegalMoves().ToList();
            var move_rankings = rank_moves(moves);

            do
            {
                int move_index = move_rankings.IndexOf(move_rankings.Max());

                Move move = moves[move_index];
                move_rankings.RemoveAt(move_index);
                moves.RemoveAt(move_index);

                board.MakeMove(move);

                double score = minimax(ref best_move, depth, max_depth, !maximizer, alpha, beta);

                best = maximizer ? Math.Max(best, score) : Math.Min(best, score);

                board.UndoMove(move);

                best_move = ((best == score) && depth == max_depth - 1) ? move : best_move;
                //best_move = (best == score) ? move : best_move;

                if (maximizer ? beta < best : alpha > best) break;

                alpha = maximizer ? Math.Max(alpha, best) : alpha;
                beta = !maximizer ? Math.Min(beta, best) : beta;

                //if (beta <= alpha) break;
            } while (move_rankings.Count > 0);

            return best;
        }

        //Console.WriteLine(minimax(true, 4, -1000, 1000) + " " + board_score(board) + " " + entries);

        //Console.WriteLine();


        // Find the next move
        /*double best_score = -1000;

        foreach (Move move in moves)
        {
            int search_depth = 3;

            entries = 0;
            board.MakeMove(move);

            if (board.IsInCheckmate()) return move;

            //if (move.CapturePieceType != PieceType.None && timer.MillisecondsRemaining > 2000) search_depth++;

            double score = search(3, true);

            if (best_score < score)
            {
                next_move = move;
                best_score = score;
            }

            board.UndoMove(move);

            Console.WriteLine(entries);
        }*/

        /*  Iterative Search  */
        Move next_move = Move.NullMove;

        /*double best = -1000;
        double alpha = double.MinValue, beta = double.MaxValue;
        int search_depth = 4;

        for (int i = 1; i <= search_depth; i++)
        {

            //Move move = Move.NullMove;
            double score = minimax(ref next_move, i, i, true, alpha, beta);

            best = Math.Max(best, score);

            //next_move = (score == best) ? move : next_move;

            //if (best == score) Console.WriteLine(best + " " + score);
        }*/

        Console.WriteLine(minimax(ref next_move, 4, 4, true));

        //Console.WriteLine(entries);


        var rand = new Random();

        next_move = (next_move == Move.NullMove) ? board.GetLegalMoves()[rand.Next(board.GetLegalMoves().Length)] : next_move;

        return next_move;
    }
}