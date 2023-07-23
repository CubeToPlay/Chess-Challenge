using ChessChallenge.API;
using System;
using System.Diagnostics.CodeAnalysis;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // Piece values:       null, pawn, knight, bishop, rook, queen, king
        int[] piece_values = { 0,    1,    3,      3,      5,    9,     10 };

        int[] get_board_values(Board input_board, bool isWhite)
        {
            int[] output_values = new int[67];

            foreach (PieceList piece_list in input_board.GetAllPieceLists())
            {
                //if (piece_list.IsWhitePieceList) continue;

                //output_values[65] += piece_values[(int)piece_list.TypeOfPieceInList] * piece_list.Count * ((piece_list.IsWhitePieceList) ? (-1) : (1)) * (board.IsWhiteToMove ? -1 : 1);

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
                            current_bit_board = BitboardHelper.GetSliderAttacks(current_piece_type, current_piece_square, input_board.IsWhiteToMove ? input_board.WhitePiecesBitboard : input_board.BlackPiecesBitboard);
                            break;
                    }

                    for (int i = 0; i < 64 ; i++)
                        output_values[i] += (((long)current_bit_board & ((Int64)1 << (63 - i))) != 0 ? 1 : 0) * (piece_list.IsWhitePieceList ? 1 : -1);
                }
            }

            //for (int i = 0; i < 64; i++)
            //{
            //    output_values[64] += output_values[i];
            //    output_values[66] += Math.Clamp(output_values[i], -1, 1);
            //}

            return output_values;
        }

        for (int i = 63; i >= 0; i--)
        {
            int value = get_board_values(board, board.IsWhiteToMove)[i];

            Console.Write(value < 0 ? "" : " ");
            Console.Write(get_board_values(board, board.IsWhiteToMove)[i]);
            if ((i) % 8 == 0)
            {
                Console.WriteLine();
            }
        }
        //Console.WriteLine();
        //Console.WriteLine(get_board_values(board, board.IsWhiteToMove)[64]);
        //Console.WriteLine(get_board_values(board, board.IsWhiteToMove)[65]);
        //Console.WriteLine(get_board_values(board, board.IsWhiteToMove)[66]);

        Console.WriteLine(get_board_values(board, board.IsWhiteToMove)[board.GetKingSquare(board.IsWhiteToMove).Index]);


        int get_king_danger(Board input_board)
        {
            int king_danger = 0;

            for (int i = 63; i >= 0; i--)
            {
                if (((long)BitboardHelper.GetKingAttacks(input_board.GetKingSquare(input_board.IsWhiteToMove)) & ((Int64)1 << i)) != 0)
                {
                    int danger_value = get_board_values(input_board, input_board.IsWhiteToMove)[i] - 1;
                    king_danger -= danger_value;
                }
            }

            return king_danger;
        }

        Move get_next_move(Board input_board)
        {
            Move[] legal_moves = input_board.GetLegalMoves();

            int[] move_scores = new int[legal_moves.Length];
            int max_score_move_index = 0;

            int index = 0;

            //Console.WriteLine(get_king_danger(input_board));

            foreach (Move move in legal_moves)
            {
                int[] board_values = get_board_values(input_board, input_board.IsWhiteToMove);
                int piece_value = piece_values[(int)move.MovePieceType];
                int king_danger = get_king_danger(input_board);

                input_board.MakeMove(move);
                if (input_board.IsInCheckmate()) return move;
                //move_scores[index] += (king_danger - get_king_danger(input_board));

                if (input_board.IsInCheck()) move_scores[index] += 3;
                move_scores[index] += (board_values[64] - get_board_values(input_board, input_board.IsWhiteToMove)[64]) / 2;
                move_scores[index] += board_values[66] - get_board_values(input_board, input_board.IsWhiteToMove)[66];

                input_board.UndoMove(move);


                //move_scores[index] += board_values[move.TargetSquare.Index] * piece_value;
                //move_scores[index] += piece_values[(int)move.CapturePieceType];
                //move_scores[index] -= Math.Max(board_values[move.StartSquare.Index], 0) * piece_value;

                index++;
            }


            index = 0;

            //Console.WriteLine();
            foreach (int move_score in move_scores)
            {
                //Console.WriteLine(move_score);
                max_score_move_index = ((move_score - move_scores[max_score_move_index]) * (board.IsWhiteToMove ? -1 : 1) > 0) ? index : max_score_move_index;
                index++;
            }
            //Console.WriteLine();

            //Console.WriteLine(move_scores[max_score_move_index]);

            return legal_moves[max_score_move_index];
        }

        return get_next_move(board);
    }
}