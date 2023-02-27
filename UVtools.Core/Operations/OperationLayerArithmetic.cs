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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


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

    public sealed class ArithmeticOperationGroup
    {
        public List<uint> SetLayers { get; set; } = new();
        public List<ArithmeticOperation> Operations { get; set; } = new();

        public bool IsValid =>
            SetLayers.Count > 0 && Operations.Count > 0 &&
            !(SetLayers.Count == 1 && Operations.Count == 1 && SetLayers[0] == Operations[0].LayerIndex);
    }
    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "fa-solid fa-square-root-alt";
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
        "Note: Calculation are made sequential, math order rules wont apply here.\n" +
        "Use ; to split and start a new arithmetic operation.";

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
        /*else if (SetLayers.Count == 0)
            sb.AppendLine("No layers to assign.");
        else if (Core.Operations.Count == 0)
            sb.AppendLine("No operations to perform.");*/
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
    public List<ArithmeticOperationGroup> Operations { get; } = new();

    public bool IsValid => Operations.Count > 0;
    #endregion

    #region Constructor

    public OperationLayerArithmetic() { }

    public OperationLayerArithmetic(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    public bool Parse()
    {
        if (string.IsNullOrEmpty(_sentence)) return false;
        Operations.Clear();

        
        var sentences = Regex.Replace(_sentence, @"\s+", string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var sentence in sentences)
        {
            var group = new ArithmeticOperationGroup();
            var splitSentence = sentence.Split('=');
            var operations = splitSentence[0];
            if (splitSentence.Length >= 2)
            {
                operations = splitSentence[1];
                var setLayers = splitSentence[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var layer in setLayers)
                {
                    var rangeSplit = layer.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (rangeSplit.Length > 1)
                    {
                        uint.TryParse(rangeSplit[0], out var startLayer);
                        if (!uint.TryParse(rangeSplit[1], out var endLayer)) endLayer = SlicerFile.LastLayerIndex;
                        SlicerFile.SanitizeLayerIndex(ref endLayer);
                        for (var index = startLayer; index <= endLayer; index++)
                        {
                            if (group.SetLayers.Contains(index)) continue;
                            group.SetLayers.Add(index);
                        }
                        continue;
                    }

                    if (!uint.TryParse(layer, out var layerIndex)) continue;
                    if (group.SetLayers.Contains(layerIndex)) continue;
                    group.SetLayers.Add(layerIndex);
                }
            }

            group.SetLayers.Sort();

            if (string.IsNullOrWhiteSpace(operations)) return false;

            string layerIndexStr = string.Empty;
            foreach (char c in operations)
            {
                if (c is >= '0' and <= '9')
                {
                    layerIndexStr += c;
                    continue;
                }

                var op = c switch
                {
                    '+' => LayerArithmeticOperators.Add,
                    '-' => LayerArithmeticOperators.Subtract,
                    '*' => LayerArithmeticOperators.Multiply,
                    '/' => LayerArithmeticOperators.Divide,
                    '&' => LayerArithmeticOperators.BitwiseAnd,
                    '|' => LayerArithmeticOperators.BitwiseOr,
                    '^' => LayerArithmeticOperators.BitwiseXor,
                    '$' => LayerArithmeticOperators.AbsDiff,
                    _ => LayerArithmeticOperators.None
                };

                if (op == LayerArithmeticOperators.None  // No valid operator
                    || string.IsNullOrWhiteSpace(layerIndexStr) // Started with a operator instead of layer
                   ) continue;


                if (uint.TryParse(layerIndexStr, out var layerIndex) && layerIndex <= SlicerFile.LastLayerIndex)
                {
                    group.Operations.Add(new ArithmeticOperation(layerIndex, op));
                }

                // Reset layer string
                layerIndexStr = string.Empty;
            }

            // Append the left over
            if (!string.IsNullOrWhiteSpace(layerIndexStr))
            {
                if (uint.TryParse(layerIndexStr, out var layerIndex) && layerIndex <= SlicerFile.LastLayerIndex)
                {
                    group.Operations.Add(new ArithmeticOperation(layerIndex, LayerArithmeticOperators.None));
                }
            }

            //if (Operations.Count == 0) return false;
            if (group.SetLayers.Count == 0 && group.Operations.Count > 0)
            {
                group.SetLayers.Add(group.Operations[0].LayerIndex);
            }

            Operations.Add(group);
        }

        return true;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (!IsValid) return false;

        foreach (var operation in Operations)
        {
            if(!operation.IsValid) continue;

            progress.PauseOrCancelIfRequested();
            using var result = SlicerFile[operation.Operations[0].LayerIndex].LayerMat;
            using var resultRoi = GetRoiOrDefault(result);
            using var imageMask = GetMask(resultRoi);

            progress.ItemCount = (uint)operation.Operations.Count;
            for (int i = 1; i < operation.Operations.Count; i++)
            {
                progress.PauseOrCancelIfRequested();
                using var image = SlicerFile[operation.Operations[i].LayerIndex].LayerMat;
                var imageRoi = GetRoiOrDefault(image);

                switch (operation.Operations[i - 1].Operator)
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

            progress.Reset("Applied layers", (uint)operation.SetLayers.Count);
            Parallel.ForEach(operation.SetLayers, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                progress.LockAndIncrement();
                if (operation.Operations.Count == 1 || HaveROIorMask)
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
        }
        

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