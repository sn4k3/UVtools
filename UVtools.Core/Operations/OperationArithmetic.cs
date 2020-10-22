/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Emgu.CV;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationArithmetic : Operation
    {
        private string _sentence;

        public enum ArithmeticOperators : byte
        {
            None,
            Add,
            Subtract,
            Multiply,
            Divide,
            BitwiseAnd,
            BitwiseOr,
            BitwiseXor
        }

        public sealed class ArithmeticOperation
        {
            public uint LayerIndex { get; }
            public ArithmeticOperators Operator { get; }

            public ArithmeticOperation(uint layerIndex, ArithmeticOperators arithmeticOperator)
            {
                LayerIndex = layerIndex;
                Operator = arithmeticOperator;
            }
        }

        public override string Title => "Arithmetic";
        public override string Description =>
            "Perform arithmetic operations over the layers pixels.\n\n" +
            "Available operators:\n" +
            " +  -  *  /  = Add, Subtract, Multiply, Divide\n" +
            " &  |  ^  = Bitwise AND, OR, XOR\n\n" +
            "Syntax: <set_to_layer_indexes> = <layer_index> <operator> <layer_index>\n" +
            "When: \"<set_to_layer_indexes> =\" is omitted, the result will assign to the first layer on the sentence.\n\n" +
            "Example 1: 10+11\n" +
            "Example 2: 10,11,12 = 11+12-10*5\n" +
            "On example 1 the layer 10 will be set with the result of layer 10 plus layer 11.\n" +
            "On example 2 the layers 10,11,12 will be set with the result of layer 11 plus 12 minus 10 all multiplied by layer 5.\n\n" +
            "Note: Calculation are made sequential, math order rules wont apply here.";

        public override string ConfirmationText =>
            $"perform this arithmetic operation";

        public override string ProgressTitle =>
            $"performing the arithmetic operations";

        public override string ProgressAction => "Calculated layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrWhiteSpace(_sentence))
                sb.AppendLine("The sentence is empty.");
            else if(!Parse())
                sb.AppendLine("Unable to parse the sentence, malformed or incomplete.");
            else if (SetLayers.Count == 0)
                sb.AppendLine("No layers to assign.");
            else if (Operations.Count == 0)
                sb.AppendLine("No operations to perform.");

            return new StringTag(sb.ToString());
        }

        public string Sentence
        {
            get => _sentence;
            set => RaiseAndSetIfChanged(ref _sentence, value);
        }

        public List<ArithmeticOperation> Operations { get; } = new List<ArithmeticOperation>();

        public List<uint> SetLayers { get; } = new List<uint>();

        public bool IsValid => SetLayers.Count > 0 & Operations.Count > 0;

        public bool Parse()
        {
            if (string.IsNullOrEmpty(_sentence)) return false;
            SetLayers.Clear();
            Operations.Clear();

            var splitSentence = _sentence.Split('=');
            var operations = splitSentence[0];
            if (splitSentence.Length >= 2)
            {
                operations = splitSentence[1];
                var setLayers = splitSentence[0].Replace(" ", string.Empty).Split(',');
                foreach (var layer in setLayers)
                {
                    if (!uint.TryParse(layer.Trim(), out var layerIndex)) continue;
                    if (SetLayers.Contains(layerIndex)) continue;
                    SetLayers.Add(layerIndex);
                }
            }

            operations = operations.Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(operations)) return false;

            string layerIndexStr = string.Empty;
            foreach (char c in operations)
            {
                if (c >= '0' && c <= '9')
                {
                    layerIndexStr += c;
                    continue;
                }

                ArithmeticOperators op = ArithmeticOperators.None;
                switch (c)
                {
                    case '+':
                        op = ArithmeticOperators.Add;
                        break;
                    case '-':
                        op = ArithmeticOperators.Subtract;
                        break;
                    case '*':
                        op = ArithmeticOperators.Multiply;
                        break;
                    case '/':
                        op = ArithmeticOperators.Divide;
                        break;
                    case '&':
                        op = ArithmeticOperators.BitwiseAnd;
                        break;
                    case '|':
                        op = ArithmeticOperators.BitwiseOr;
                        break;
                    case '^':
                        op = ArithmeticOperators.BitwiseXor;
                        break;
                }

                if (op == ArithmeticOperators.None  // No valid operator
                    || string.IsNullOrWhiteSpace(layerIndexStr) // Started with a operator instead of layer
                    ) continue; 


                if (uint.TryParse(layerIndexStr, out var layerIndex))
                {
                    Operations.Add(new ArithmeticOperation(layerIndex, op));
                }

                // Reset layer string
                layerIndexStr = string.Empty;
            }

            // Append the left over
            if (!string.IsNullOrWhiteSpace(layerIndexStr))
            {
                if (uint.TryParse(layerIndexStr, out var layerIndex))
                {
                    Operations.Add(new ArithmeticOperation(layerIndex, ArithmeticOperators.None));
                }
            }

            if (Operations.Count == 0) return false;
            if (SetLayers.Count == 0)
            {
                SetLayers.Add(Operations[0].LayerIndex);
            }

            return true;
        }

        public OperationArithmetic()
        {
            
        }
    }
}
