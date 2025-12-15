using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
public class MyBot : IChessBot
{
    const bool debug = true;
    const bool verbose = false;
    const int maxDepth = 2;
    const int infinity = 10000;
    int nodesSearched = 0;
    int nodesPruned = 0;

    public Move Think(Board board, Timer timer)
    {
        Move[] rootMoves = board.GetLegalMoves();
        Move nextMove = rootMoves[0];
        int score = 0;
        int alpha = -infinity; // the highest score that max player (us) can guarantee thus far, upper bound
        int beta = infinity; // the lowest score that min player (them) can guarantee thus far, lower bound
        int moveScore = 0;
        nodesSearched = 0;

        if (debug)
            Console.WriteLine(board.CreateDiagram() + "\n- - - - - - - - - - - - - - - - ");

        // searching children of root node
        foreach (Move move in rootMoves)
        {
            board.MakeMove(move);
            moveScore = -NegaMax(maxDepth - 1, - beta, -alpha);

            if(debug && verbose)
                Console.WriteLine(board.CreateDiagram() + "\nEval: " + moveScore);
            else if (debug)
                Console.WriteLine("\n" + board.GetFenString() + "\nEval: " + moveScore);

            board.UndoMove(move);

            if (moveScore > score)
            {
                score = moveScore;
                nextMove = move;
            }
        }

        int NegaMax(int depth, int alpha, int beta)
        {
            if (depth == 0)
                return Evaluate(board);

            Move[] moves = board.GetLegalMoves();
            int moveScore = 0;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                nodesSearched++;
                moveScore = -NegaMax(depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (moveScore >= beta) // its already guaranteed that the min player won't go down this line
                {
                    nodesPruned++;
                    break;
                }

                alpha = Math.Max(alpha, moveScore);
            }
            return moveScore;
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

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
            {
                return -10000;
            }
            else if (board.IsWhiteToMove)
            {
                return 10000;
            }
        }
        else if (board.IsDraw())
        {
            return 0;
        }

        foreach (var pieceList in board.GetAllPieceLists())
        {
            foreach (var piece in pieceList)
            {
                int index = piece.IsWhite ? piece.Square.File + piece.Square.Rank * 8 : (piece.Square.File) + (7 - piece.Square.Rank) * 8;
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