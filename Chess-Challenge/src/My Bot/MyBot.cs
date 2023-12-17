using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json;
using System.Transactions;
using System.Xml.Serialization;

public class MyBot : IChessBot
{
    Random random = new();
    int[] piece_scores = { 0, 70, 310, 330, 500, 790, 1000 };

    public int board_score(Board board)
    {
        int score = 0;

        score += (Convert.ToInt32(board.IsInCheckmate()) * 100 - Convert.ToInt32(board.IsDraw()) * 100);



        for (int i = 0; i < 64; i++)
        {
            Piece current_piece = board.GetPiece(new(i));
            //if (current_piece.IsNull) continue;

            int perspective = (current_piece.IsWhite != board.IsWhiteToMove) ? 1 : -1;


            score += piece_scores[(int)current_piece.PieceType] * perspective;
        }

        return score;
    }

    public class Node
    {
        public int
            visits,
            value;

        public Board board;

        public Node parent, root;

        public List<Node> children;

        public Node(ref Board t_board, ref Node t_parent, ref Node t_root)
        {
            board = t_board;

            visits = 0;
            value = 0;

            parent = t_parent;
            root = t_root;
            children = new List<Node>();
        }

        public float children_average()
        {
            float result = 0;

            foreach(Node child in children)
            {
                result += child.value;
            }

            result /= children.Count;

            return result;
         }

        public static Node Null;
    }

    public Node best_child_ucb(Node parent)
    {
        Node best_child = parent.children[0];

        double best_ucb = double.MinValue;
        foreach (Node child in parent.children)
        {
            double ucb = child.visits == 0 ? double.MaxValue : parent.children_average() + 2 * Math.Sqrt(Math.Log(parent.visits) / child.visits);

            if (ucb > best_ucb) { best_ucb = ucb;  best_child = child; }
        }

        return best_child;
    }

    public int rollout_node(Board board, ref int depth)
    {
        depth++;
        var moves = board.GetLegalMoves();

        if (board.IsDraw() || board.IsInCheckmate()) return board_score(board);

        var move = moves[random.Next(moves.Length)];

        board.MakeMove(move);
        int result = rollout_node(board, ref depth);
        board.UndoMove(move);

        return result;
    }

    public void extend_node(ref Node parent, Board board)
    {
        var moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            parent.children.Add(new(ref board, ref parent, ref parent.root));

            board.UndoMove(move);
        }
    }

    public Move Think(Board board, Timer timer)
    {
        
        Move best_move = Move.NullMove;

        Node current = new(ref board, ref Node.Null, ref Node.Null);
        current.root = current;

        while (timer.MillisecondsElapsedThisTurn < 1000)
        {
            if (current.children.Count == 0) // Is leaf node
            {
                if (current.visits == 0) // Run rollout
                {
                    int depth = 0;
                    int result = rollout_node(current.board, ref depth);

                    current.value = result;
                    current.visits += 1;

                    if (current.root != null)
                    {
                        current.root.visits += 1;
                        current.root.value += result;
                    }


                    /*if (current.parent != null)
                    {
                        current.parent.visits++;
                    }*/
                }
                else
                {
                    extend_node(ref current, board);
                }
            }
            else
            {
                current = best_child_ucb(current);
            }

            
        }

        Console.WriteLine(current.root.children.Count);

        //return board.GetLegalMoves()[random.Next(0, board.GetLegalMoves().Length - 1)];
        return best_move;
    }
}