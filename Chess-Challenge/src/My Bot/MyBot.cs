using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ChessChallenge.API.BitboardHelper;
public class MyBot : IChessBot
{
    const bool debug = false;
    const bool verbose = false;
    const bool clearConsoleOnMove = true;
    const int maxDepth = 4;
    const int infinity = 10000;
    int nodesSearched = 0;
    int nodesPruned = 0;

    const int pawnMobilityWeight = 1;
    const int knightMobilityWeight = 8;
    const int bishopMobilityWeight = 8;
    const int rookMobilityWeight = 4;
    const int queenMobilityWeight = 4;
    const int kingMobilityWeight = -1;

    public Move Think(Board board, Timer timer)
    {
        if (debug && clearConsoleOnMove)
            Console.Clear();

        Move[] rootMoves = OrderMoves(board, board.GetLegalMoves());
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
            Move[] moves = OrderMoves(board, board.GetLegalMoves());

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

                return eval;
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

                if (debug && Math.Abs(moveScore) == 9998)
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

    Move[] OrderMoves(Board board, Move[] moves)
    {
        return moves.OrderByDescending(x => GetMovePriority(board, x)).ToArray();
    }

    int GetMovePriority(Board board, Move move)
    {
        int movePriority = 0;

        // check if the move is the capture of a more valuable piece with a less valuable piece
        movePriority += 10 * GetPieceValue(move.CapturePieceType) - GetPieceValue(move.MovePieceType);

        // check if move is a pawn promotion
        if (move.IsPromotion)
        {
            movePriority += GetPieceValue(move.PromotionPieceType);
        }

        return movePriority;
    }

    int GetPieceValue(PieceType piece)
    {
        int[] pieceValues = { 0, 100, 310, 330, 510, 880, 10000 };

        return pieceValues[(int)piece];
    }

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] mg_pawn_table = {
              0,   0,   0,   0,   0,   0,  0,   0,
             98, 134,  61,  95,  68, 126, 34, -11,
             -6,   7,  26,  31,  65,  56, 25, -20,
            -14,  13,   6,  21,  23,  12, 17, -23,
            -27,  -2,  -5,  12,  17,   6, 10, -25,
            -26,  -4,  -4, -10,   3,   3, 33, -12,
            -35,  -1, -20, -23, -15,  24, 38, -22,
              0,   0,   0,   0,   0,   0,  0,   0,
        };

    int[] eg_pawn_table = {
              0,   0,   0,   0,   0,   0,   0,   0,
            178, 173, 158, 134, 147, 132, 165, 187,
             94, 100,  85,  67,  56,  53,  82,  84,
             32,  24,  13,   5,  -2,   4,  17,  17,
             13,   9,  -3,  -7,  -7,  -8,   3,  -1,
              4,   7,  -6,   1,   0,  -5,  -1,  -8,
             13,   8,   8,  10,  13,   0,   2,  -7,
              0,   0,   0,   0,   0,   0,   0,   0,
        };

    int[] mg_knight_table = {
            -167, -89, -34, -49,  61, -97, -15, -107,
             -73, -41,  72,  36,  23,  62,   7,  -17,
             -47,  60,  37,  65,  84, 129,  73,   44,
              -9,  17,  19,  53,  37,  69,  18,   22,
             -13,   4,  16,  13,  28,  19,  21,   -8,
             -23,  -9,  12,  10,  19,  17,  25,  -16,
             -29, -53, -12,  -3,  -1,  18, -14,  -19,
            -105, -21, -58, -33, -17, -28, -19,  -23,
        };

    int[] eg_knight_table = {
            -58, -38, -13, -28, -31, -27, -63, -99,
            -25,  -8, -25,  -2,  -9, -25, -24, -52,
            -24, -20,  10,   9,  -1,  -9, -19, -41,
            -17,   3,  22,  22,  22,  11,   8, -18,
            -18,  -6,  16,  25,  16,  17,   4, -18,
            -23,  -3,  -1,  15,  10,  -3, -20, -22,
            -42, -20, -10,  -5,  -2, -20, -23, -44,
            -29, -51, -23, -15, -22, -18, -50, -64,
        };

    int[] mg_bishop_table = {
            -29,   4, -82, -37, -25, -42,   7,  -8,
            -26,  16, -18, -13,  30,  59,  18, -47,
            -16,  37,  43,  40,  35,  50,  37,  -2,
             -4,   5,  19,  50,  37,  37,   7,  -2,
             -6,  13,  13,  26,  34,  12,  10,   4,
              0,  15,  15,  15,  14,  27,  18,  10,
              4,  15,  16,   0,   7,  21,  33,   1,
            -33,  -3, -14, -21, -13, -12, -39, -21,
        };

    int[] eg_bishop_table = {
            -14, -21, -11,  -8, -7,  -9, -17, -24,
             -8,  -4,   7, -12, -3, -13,  -4, -14,
              2,  -8,   0,  -1, -2,   6,   0,   4,
             -3,   9,  12,   9, 14,  10,   3,   2,
             -6,   3,  13,  19,  7,  10,  -3,  -9,
            -12,  -3,   8,  10, 13,   3,  -7, -15,
            -14, -18,  -7,  -1,  4,  -9, -15, -27,
            -23,  -9, -23,  -5, -9, -16,  -5, -17,
        };

    int[] mg_rook_table = {
             32,  42,  32,  51, 63,  9,  31,  43,
             27,  32,  58,  62, 80, 67,  26,  44,
             -5,  19,  26,  36, 17, 45,  61,  16,
            -24, -11,   7,  26, 24, 35,  -8, -20,
            -36, -26, -12,  -1,  9, -7,   6, -23,
            -45, -25, -16, -17,  3,  0,  -5, -33,
            -44, -16, -20,  -9, -1, 11,  -6, -71,
            -19, -13,   1,  17, 16,  7, -37, -26,
        };

    int[] eg_rook_table = {
            13, 10, 18, 15, 12,  12,   8,   5,
            11, 13, 13, 11, -3,   3,   8,   3,
             7,  7,  7,  5,  4,  -3,  -5,  -3,
             4,  3, 13,  1,  2,   1,  -1,   2,
             3,  5,  8,  4, -5,  -6,  -8, -11,
            -4,  0, -5, -1, -7, -12,  -8, -16,
            -6, -6,  0,  2, -9,  -9, -11,  -3,
            -9,  2,  3, -1, -5, -13,   4, -20,
        };

    int[] mg_queen_table = {
            -28,   0,  29,  12,  59,  44,  43,  45,
            -24, -39,  -5,   1, -16,  57,  28,  54,
            -13, -17,   7,   8,  29,  56,  47,  57,
            -27, -27, -16, -16,  -1,  17,  -2,   1,
             -9, -26,  -9, -10,  -2,  -4,   3,  -3,
            -14,   2, -11,  -2,  -5,   2,  14,   5,
            -35,  -8,  11,   2,   8,  15,  -3,   1,
             -1, -18,  -9,  10, -15, -25, -31, -50,
        };

    int[] eg_queen_table = {
             -9,  22,  22,  27,  27,  19,  10,  20,
            -17,  20,  32,  41,  58,  25,  30,   0,
            -20,   6,   9,  49,  47,  35,  19,   9,
              3,  22,  24,  45,  57,  40,  57,  36,
            -18,  28,  19,  47,  31,  34,  39,  23,
            -16, -27,  15,   6,   9,  17,  10,   5,
            -22, -23, -30, -16, -16, -23, -36, -32,
            -33, -28, -22, -43,  -5, -32, -20, -41,
        };

    int[] mg_king_table = {
            -65,  23,  16, -15, -56, -34,   2,  13,
             29,  -1, -20,  -7,  -8,  -4, -38, -29,
             -9,  24,   2, -16, -20,   6,  22, -22,
            -17, -20, -12, -27, -30, -25, -14, -36,
            -49,  -1, -27, -39, -46, -44, -33, -51,
            -14, -14, -22, -46, -44, -30, -15, -27,
              1,   7,  -8, -64, -43, -16,   9,   8,
            -15,  86,  62, -54,  58, -28,  74,  64, // edited slightly
        };

    int[] eg_king_table = {
            -74, -35, -18, -18, -11,  15,   4, -17,
            -12,  17,  14,  17,  17,  38,  23,  11,
             10,  17,  23,  15,  20,  45,  44,  13,
             -8,  22,  24,  27,  26,  33,  26,   3,
            -18,  -4,  21,  24,  27,  23,   9, -11,
            -19,  -3,  11,  21,  23,  16,   7,  -9,
            -27, -11,   4,  13,  14,   4,  -5, -17,
            -53, -34, -21, -11, -28, -14, -24, -43
        };

    float EndgameProgression(Board board)
    {
        return Math.Min(1f, 1f - ((float)GetNumberOfSetBits(board.AllPiecesBitboard) / 32f) + 0.25f); // how near are we to the endgame?
    }
    int Evaluate(Board board)
    {
        int[] pieceValues = { 100, 310, 330, 510, 880, 10000 };
        int eval = 0;
        float endgameProgression = EndgameProgression(board);

        if (board.IsWhiteToMove)
            eval += 10;
        else
            eval -= 10;

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
                        eval += (int)MathF.Round(mg_pawn_table[index] / endgameProgression +
                            (float)eg_pawn_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetPawnAttacks(piece.Square, piece.IsWhite)) * mult * pawnMobilityWeight;
                        break;
                    case PieceType.Knight:
                        eval += pieceValues[1] * mult;
                        eval += (int)MathF.Round((float)mg_knight_table[index] / endgameProgression +
                            (float)eg_knight_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetKnightAttacks(piece.Square)) * mult * knightMobilityWeight;
                        break;
                    case PieceType.Bishop:
                        eval += pieceValues[2] * mult;
                        eval += (int)MathF.Round((float)mg_bishop_table[index] / endgameProgression +
                            (float)eg_bishop_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetSliderAttacks(PieceType.Bishop, piece.Square, board)) * mult * bishopMobilityWeight;
                        break;
                    case PieceType.Rook:
                        eval += pieceValues[3] * mult;
                        eval += (int)MathF.Round((float)mg_rook_table[index] / endgameProgression +
                            (float)eg_rook_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetSliderAttacks(PieceType.Rook, piece.Square, board)) * mult * rookMobilityWeight;
                        break;
                    case PieceType.Queen:
                        eval += pieceValues[4] * mult;
                        eval += (int)MathF.Round((float)mg_queen_table[index] / endgameProgression +
                            (float)eg_queen_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetSliderAttacks(PieceType.Queen, piece.Square, board)) * mult * queenMobilityWeight;
                        break;
                    case PieceType.King:
                        eval += pieceValues[5] * mult;
                        eval += (int)MathF.Round((float)mg_king_table[index] / endgameProgression +
                            (float)eg_king_table[index] * endgameProgression) * mult / 2;
                        eval += GetNumberOfSetBits(GetKingAttacks(piece.Square)) * mult * kingMobilityWeight;
                        break;
                }
            }
        }

        return board.IsWhiteToMove ? eval : -eval;
    }
}