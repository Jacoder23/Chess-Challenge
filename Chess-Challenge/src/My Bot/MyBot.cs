using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
public class MyBot : IChessBot
{
    const bool debug = true;
    const bool verbose = false;
    const bool clearConsoleOnMove = true;
    const int maxDepth = 2;
    const int infinity = 10000;
    int nodesSearched = 0;
    int nodesPruned = 0;

    public Move Think(Board board, Timer timer)
    {
        if (clearConsoleOnMove)
            Console.Clear();

        Move[] rootMoves = board.GetLegalMoves();
        Move nextMove = rootMoves[0];
        int score = -infinity;
        int alpha = -infinity; // the highest score that max player (us) can guarantee thus far
        int beta = infinity; // the lowest score that min player (them) can guarantee thus far
        nodesSearched = 0;

        if (debug)
            Console.WriteLine(board.CreateDiagram());

        NegaMax(maxDepth, -beta, -alpha);

        int NegaMax(int depth, int alpha, int beta)
        {
            Move[] moves = board.GetLegalMoves();

            if (depth == 0 || moves.Length == 0)
            {
                int eval = 0;
                bool isMate = board.IsInCheckmate();

                if (isMate)
                {
                    if (board.IsWhiteToMove)
                    {
                        eval = 10000 - (maxDepth - depth);
                    }
                    else if (!board.IsWhiteToMove)
                    {
                        eval = -10000 + (maxDepth - depth);
                    }
                }
                else if (board.IsDraw())
                {
                    eval = 0;
                }
                else
                {
                    eval = Evaluate(board);
                }

                if (debug && verbose)
                    Console.WriteLine("- - - - - - - - - - - - - - - - " + "\nLeaf node" + "  | Eval: " + eval + (isMate ? " (Checkmate)" : "") + "\n" + board.CreateDiagram());
                else if (debug)
                    Console.WriteLine("- - - - - - - - - - - - - - - - " + "\nLeaf node" + "  | Eval: " + eval + (isMate ? " (Checkmate)" : "") + "\n" + board.GetFenString());

                return eval; // not propogating back properly
            }

            int moveScore = 0;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                nodesSearched++;
                if (debug)
                    Console.WriteLine("                        " + move.ToString() + " at ply of " + (maxDepth - depth + 1));
                moveScore = -NegaMax(depth - 1, -beta, -alpha);
                if(debug && depth == maxDepth)
                    Console.WriteLine("                        Root Child: " + move.ToString() + " |  " + moveScore + " |  " + "Next move: " + nextMove.ToString());
                board.UndoMove(move);

                if (moveScore >= beta) // its already guaranteed that the min player won't go down this line
                {
                    nodesPruned++;
                    if (debug)
                        Console.WriteLine("                        Node pruned. Score was " + moveScore + " and beta was " + beta + ".");
                    return beta;
                }

                if (moveScore > score && depth == maxDepth)
                {
                    nextMove = move;
                    score = moveScore;
                }

                if (Math.Abs(moveScore) == 9998)
                    Console.WriteLine("Alpha is " + alpha);

                if (moveScore > alpha)
                {
                    alpha = moveScore;
                }

                if(depth == maxDepth)
                {
                    if (debug && verbose)
                        Console.WriteLine(board.CreateDiagram() + move.ToString() + "\nEval: " + moveScore);
                    else if (debug)
                        Console.WriteLine("\n" + board.GetFenString() + "\n" + move.ToString() + "\nEval: " + moveScore);
                }
            }
            return alpha;
        }

        if (debug)
        {
            Console.WriteLine("\nNodes searched: " + nodesSearched);
            Console.WriteLine("Nodes pruned: " + nodesPruned);
            Console.WriteLine("Chosen move: " + nextMove.ToString());
            Console.WriteLine("Current eval: " + score);
        }

        return nextMove;
    }

    int Evaluate(Board board)
    {
        int[] pieceValues = { 100, 310, 330, 510, 880, 10000 };
        int eval = 0;

        foreach (var pieceList in board.GetAllPieceLists())
        {
            foreach (var piece in pieceList)
            {
                int mult = piece.IsWhite ? 1 : -1;
                switch (piece.PieceType)
                {
                    case PieceType.Pawn:
                        eval += pieceValues[0] * mult;
                        break;
                    case PieceType.Knight:
                        eval += pieceValues[1] * mult;
                        break;
                    case PieceType.Bishop:
                        eval += pieceValues[2] * mult;
                        break;
                    case PieceType.Rook:
                        eval += pieceValues[3] * mult;
                        break;
                    case PieceType.Queen:
                        eval += pieceValues[4] * mult;
                        break;
                    case PieceType.King:
                        eval += pieceValues[5] * mult;
                        break;
                }
            }
        }

        return board.IsWhiteToMove ? eval : -eval;
    }
}