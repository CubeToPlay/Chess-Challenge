using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Random random = new Random();
        Move[] moves = board.GetLegalMoves();

        int search_depth = 5;

        // Piece values:       null, pawn, knight, bishop, rook, queen, king
        int[] piece_values = { 0, 1, 4, 5, 5, 9, 10 };

        void swap(ref double a, ref double b)
        {
            double t = a;

            a = b;
            b = t;
        }

        void quicksort(ref double[] score_array, ref double[] index_array, int l, int h)
        {
            if (l < h)
            {
                int t = l - 1;

                for (int i = l; i <= h - 1; i++)
                {
                    if (score_array[i] <= score_array[h])
                    {
                        t++;
                        swap(ref score_array[i], ref score_array[t]);
                        swap(ref index_array[i], ref index_array[t]);
                    }
                }

                t++;
                swap(ref score_array[t], ref score_array[h]);
                swap(ref index_array[t], ref index_array[h]);

                quicksort(ref score_array, ref index_array, l, t - 1);
                quicksort(ref score_array, ref index_array, t + 1, h);
            }
        }

        double[] sort_moves(ref Move[] input_moves, Board input_board)
        {
            double[]
                move_scores = new double[input_moves.Length],
                index_array = new double[input_moves.Length];



            for (int i = 0; i < input_moves.Length; i++)
            {
                index_array[i] = i;
                move_scores[i] = get_move_score(input_board, input_moves[i]);
            }

            quicksort(ref move_scores, ref index_array, 0, input_moves.Length - 1);

            return index_array;
        }

        double[] get_values(Board input_board)
        {
            double[] output_values = new double[67];

            foreach (PieceList piece_list in input_board.GetAllPieceLists())
            {
                PieceType piece_type = piece_list.TypeOfPieceInList;
                int piece_sign_value = piece_list.IsWhitePieceList ? -1 : 1;
                int board_sign_value = input_board.IsWhiteToMove ? 1 : -1;
                output_values[65] += piece_values[(int)piece_type] * piece_list.Count * board_sign_value * piece_sign_value;

                foreach (Piece piece in piece_list)
                {
                    PieceType current_piece_type = piece.PieceType;
                    Square current_piece_square = piece.Square;
                    ulong current_bit_board = 0;

                    switch (current_piece_type)
                    {
                        case PieceType.Pawn:
                            current_bit_board = BitboardHelper.GetPawnAttacks(current_piece_square, piece_list.IsWhitePieceList);
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
                            current_bit_board = BitboardHelper.GetSliderAttacks(current_piece_type, current_piece_square, input_board.AllPiecesBitboard);
                            break;
                    }

                    for (int i = 0; i < 64; i++)
                        output_values[i] += (((long)current_bit_board & ((Int64)1 << i)) != 0 ? 1 : 0) * piece_sign_value * board_sign_value;
                }
            }

            for (int i = 0; i < 64; i++)
            {
                output_values[64] += output_values[i];
                output_values[66] += Math.Clamp(output_values[i], -1, 1);
            }

            return output_values;
        }

        double get_score(Board input_board)
        {
            double[] board_values = get_values(input_board);

            return
                (
                board_values[64] / 3 + // square control
                board_values[65] * 2 + // piece difference
                board_values[66] / 2 + // board control
                (input_board.IsInCheckmate() ? 100 : 0) +
                (input_board.IsDraw() ? -50 : 0) +
                (input_board.IsInCheck() ? 2 : 0)
                );
        }

        double get_move_score(Board input_board, Move next_move)
        {
            double
                move_score = 0,
                move_piece_value = piece_values[(int)next_move.MovePieceType],
                capture_piece_value = piece_values[(int)next_move.CapturePieceType];
            double[] input_board_values = get_values(input_board);

            input_board.MakeMove(next_move);
            move_score += get_values(input_board)[64] - input_board_values[64];
            move_score += get_values(input_board)[65] - input_board_values[65];
            //move_score -= get_values(input_board)[next_move.TargetSquare.Index];
            move_score += input_board.IsInCheckmate() ? 100 : 0;
            //move_score -= get_values(input_board)[next_move.StartSquare.Index] * move_piece_value;
            input_board.UndoMove(next_move);

            //move_score += capture_piece_value;
            //move_score += input_board.SquareIsAttackedByOpponent(next_move.TargetSquare) ? capture_piece_value - move_piece_value : capture_piece_value;
            move_score += input_board.SquareIsAttackedByOpponent(next_move.StartSquare) ? move_piece_value : 0;

            return move_score;
        }

        double[] minimax(int depth, int index, bool maximizer, int origin_index, double alpha, double beta, int max_depth, Board input_board)
        {
            Move[] input_moves = input_board.GetLegalMoves();

            //Console.WriteLine(max_depth);

            if (timer.MillisecondsRemaining <= 20000 || input_board.GetLegalMoves(true).Length == 0) max_depth = 3;

            if (depth == 1) origin_index = index;

            if (depth >= max_depth || input_board.IsInCheckmate() || input_board.IsDraw()) return new double[2] { get_score(input_board), origin_index}; 

            double[] 
                best_result = new double[2],
                current_result = new double[2];

            best_result[0] = maximizer ? -1000 : 1000;

            double[] index_array = sort_moves(ref input_moves, input_board);

            depth += 1;
            for (int i = input_moves.Length - 1; i >= 0; i--)
            {
                int current_index = (int)index_array[i];

                input_board.MakeMove(input_moves[current_index]);

                //if (input_moves[current_index].CapturePieceType == PieceType.None) max_depth = 3;

                //max_depth = (input_moves[current_index].CapturePieceType != PieceType.None) ? 5 : 3;

                current_result = minimax(depth, current_index, !maximizer, origin_index, alpha, beta, max_depth, input_board);
                best_result[0] = maximizer ? Math.Max(best_result[0], current_result[0]) : Math.Min(best_result[0], current_result[0]);
                best_result[1] = current_result[0] == best_result[0] ? current_result[1] : best_result[1];

                input_board.UndoMove(input_moves[current_index]);

                bool skip = maximizer ? best_result[0] > beta : best_result[0] < alpha;

                if (skip) break;

                alpha = maximizer ? Math.Max(alpha, best_result[0]) : alpha;
                beta = !maximizer ? Math.Min(beta, best_result[0]) : beta;
            }

            return best_result;
        }


        double[] minimax_values = minimax(0, 0, true, 0, -1000, 1000, search_depth, board);

        //Console.WriteLine(checkmate_number);

        //Console.WriteLine(minimax_values[0] + " " + minimax_values[1]);

        //Console.WriteLine(get_values(board)[64] + " " + get_values(board)[65] + " " + get_values(board)[66]);

        Move next_move = moves[(int)minimax_values[1]];

        //Console.WriteLine(get_move_score(board, next_move));

        return next_move;
    }
}