/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationLayerArithmetic : Operation
{
    #region Members
    private string _sentence = null!;
    #endregion

    #region Enums
    public enum LayerArithmeticOperators : byte
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        AbsDiff
    }
    #endregion

    #region SubClasses
    public sealed class ArithmeticOperation
    {
        public uint LayerIndex { get; }
        public LayerArithmeticOperators Operator { get; }

        public ArithmeticOperation(uint layerIndex, LayerArithmeticOperators layerArithmeticOperator)
        {
            LayerIndex = layerIndex;
            Operator = layerArithmeticOperator;
        }
    }
    #endregion

    #region Overrides

    public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
    public override string IconClass => "fas fa-square-root-alt";
    public override string Title => "Layer arithmetic";
    public override string Description =>
        "Perform arithmetic operations over the layers\n" +
        "Available operators:\n" +
        " +  -  *  /  = Add, Subtract, Multiply, Divide\n" +
        " &  |  ^  = Bitwise AND, OR, XOR\n" +
        " $ = Absolute difference\n\n" +
        "Syntax: <set_to_layer_indexes> = <layer_index> <operator> <layer_index>\n" +
        "When: \"<set_to_layer_indexes> =\" is omitted, the result will assign to the first layer on the sentence.\n\n" +
        "Example 1: 10+11\n" +
        "Example 2: 10,11,12 = 11+12-10*5   Same as: 10:12 = 11+12-10*5\n" +
        "On example 1 the layer 10 will be set with the result of layer 10 plus layer 11.\n" +
        "On example 2 the layers 10,11,12 will be set with the result of layer 11 plus 12 minus 10 all multiplied by layer 5.\n\n" +
        "Note: Calculation are made sequential, math order rules wont apply here.";

    public override string ConfirmationText =>
        $"perform this arithmetic operation";

    public override string ProgressTitle =>
        $"performing the arithmetic operations";

    public override string ProgressAction => "Calculated layers";

    public override string? ValidateInternally()
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
        else if (!IsValid)
            sb.AppendLine("The operation will have no impact and will not be performed.");

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{_sentence}";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties
    public string Sentence
    {
        get => _sentence;
        set => RaiseAndSetIfChanged(ref _sentence, value);
    }
    [XmlIgnore]
    public List<ArithmeticOperation> Operations { get; private set; } = new();

    [XmlIgnore]
    public List<uint> SetLayers { get; private set; } = new();

    public bool IsValid => SetLayers.Count > 0 && Operations.Count > 0 && 
                           !(SetLayers.Count == 1 && Operations.Count == 1 && SetLayers[0] == Operations[0].LayerIndex);
    #endregion

    #region Constructor

    public OperationLayerArithmetic() { }

    public OperationLayerArithmetic(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

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
            var setLayers = splitSentence[0].Replace(" ", string.Empty).Split(',', StringSplitOptions.TrimEntries);
            foreach (var layer in setLayers)
            {
                var rangeSplit = layer.Split(':', StringSplitOptions.TrimEntries);
                if (rangeSplit.Length > 1)
                {
                    uint.TryParse(rangeSplit[0].Trim(), out var startLayer);
                    if (!uint.TryParse(rangeSplit[1].Trim(), out var endLayer)) endLayer = SlicerFile.LastLayerIndex;
                    for (var index = startLayer; index <= endLayer; index++)
                    {
                        if (SetLayers.Contains(index)) continue;
                        SetLayers.Add(index);
                    }
                    continue;
                }

                if (!uint.TryParse(layer.Trim(), out var layerIndex)) continue;
                if (SetLayers.Contains(layerIndex)) continue;
                SetLayers.Add(layerIndex);
            }
        }

        SetLayers = SetLayers.Where(layerIndex => layerIndex <= SlicerFile.LastLayerIndex).ToList();
        SetLayers.Sort();

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

            LayerArithmeticOperators op = LayerArithmeticOperators.None;
            switch (c)
            {
                case '+':
                    op = LayerArithmeticOperators.Add;
                    break;
                case '-':
                    op = LayerArithmeticOperators.Subtract;
                    break;
                case '*':
                    op = LayerArithmeticOperators.Multiply;
                    break;
                case '/':
                    op = LayerArithmeticOperators.Divide;
                    break;
                case '&':
                    op = LayerArithmeticOperators.BitwiseAnd;
                    break;
                case '|':
                    op = LayerArithmeticOperators.BitwiseOr;
                    break;
                case '^':
                    op = LayerArithmeticOperators.BitwiseXor;
                    break;
                case '$':
                    op = LayerArithmeticOperators.AbsDiff;
                    break;
            }

            if (op == LayerArithmeticOperators.None  // No valid operator
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
                Operations.Add(new ArithmeticOperation(layerIndex, LayerArithmeticOperators.None));
            }
        }

        Operations = Operations.Where(op => op.LayerIndex <= SlicerFile.LastLayerIndex).ToList();

        //if (Operations.Count == 0) return false;
        if (SetLayers.Count == 0 && Operations.Count > 0)
        {
            SetLayers.Add(Operations[0].LayerIndex);
        }

        return true;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (!IsValid) return false;

        using var result = SlicerFile[Operations[0].LayerIndex].LayerMat;
        using var resultRoi = GetRoiOrDefault(result);
        using var imageMask = GetMask(resultRoi);

        progress.ItemCount = (uint) Operations.Count;
        for (int i = 1; i < Operations.Count; i++)
        {
            progress.ThrowIfCancellationRequested();
            using var image = SlicerFile[Operations[i].LayerIndex].LayerMat;
            var imageRoi = GetRoiOrDefault(image);
                
            switch (Operations[i - 1].Operator)
            {
                case LayerArithmeticOperators.Add:
                    CvInvoke.Add(resultRoi, imageRoi, resultRoi, imageMask);
                    break;
                case LayerArithmeticOperators.Subtract:
                    CvInvoke.Subtract(resultRoi, imageRoi, resultRoi, imageMask);
                    break;
                case LayerArithmeticOperators.Multiply:
                    CvInvoke.Multiply(resultRoi, imageRoi, resultRoi);
                    break;
                case LayerArithmeticOperators.Divide:
                    CvInvoke.Divide(resultRoi, imageRoi, resultRoi);
                    break;
                case LayerArithmeticOperators.BitwiseAnd:
                    CvInvoke.BitwiseAnd(resultRoi, imageRoi, resultRoi, imageMask);
                    break;
                case LayerArithmeticOperators.BitwiseOr:
                    CvInvoke.BitwiseOr(resultRoi, imageRoi, resultRoi, imageMask);
                    break;
                case LayerArithmeticOperators.BitwiseXor:
                    CvInvoke.BitwiseXor(resultRoi, imageRoi, resultRoi, imageMask);
                    break;
                case LayerArithmeticOperators.AbsDiff:
                    CvInvoke.AbsDiff(resultRoi, imageRoi, resultRoi);
                    break;
            }

            progress++;
        }

        progress.Reset("Applied layers", (uint) SetLayers.Count);
        Parallel.ForEach(SetLayers, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.LockAndIncrement();
            if (Operations.Count == 1 || HaveROIorMask)
            {
                using var mat = SlicerFile[layerIndex].LayerMat;
                var matRoi = GetRoiOrDefault(mat);
                resultRoi.CopyTo(matRoi, imageMask);
                SlicerFile[layerIndex].LayerMat = mat;
                return;
            }

            //ApplyMask(mat, resultRoi, imageMask);

            SlicerFile[layerIndex].LayerMat = result;
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality
    protected bool Equals(OperationLayerArithmetic other)
    {
        return _sentence == other._sentence;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationLayerArithmetic) obj);
    }

    public override int GetHashCode()
    {
        return (_sentence != null ? _sentence.GetHashCode() : 0);
    }
    #endregion
}