using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using TMPro;

public class num_Scene : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("UI References")]
    public InputField inputField; // 文本输入框
    public TextMeshProUGUI displayText;      // 文本显示框
    
    [Header("Settings")]
    public float orientationThreshold = 0.5f; // 方向检测阈值

    private Fraction currentFraction;
    private bool isUpsideDown = false;

    void Start()
    {
        // 初始化重力传感器
        Input.gyro.enabled = true;
        
        // 设置输入框监听
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputChanged);
            // 设置默认值
            inputField.text = "1/2";
        }
        
        // 初始显示
        UpdateDisplay();
    }

    void Update()
    {
        // 检测手机方向
        bool newOrientation = IsPhoneUpsideDown();
        
        // 如果方向发生变化，更新显示
        if (newOrientation != isUpsideDown)
        {
            isUpsideDown = newOrientation;
            UpdateDisplay();
        }
    }

    // 输入变化回调
    private void OnInputChanged(string input)
    {
        try
        {
            currentFraction = ParseInput(input);
            UpdateDisplay();
        }
        catch (Exception e)
        {
            if (displayText != null)
                displayText.text = $"错误: {e.Message}";
        }
    }

    // 检测手机是否倒置
    private bool IsPhoneUpsideDown()
    {
        // 使用重力传感器的Y轴分量判断
        // 正置时Y接近-1，倒置时Y接近+1
        return Input.acceleration.y > orientationThreshold;
    }

    // 更新显示
    private void UpdateDisplay()
    {
        if (displayText == null) return;
        if (isUpsideDown)
        {
            displayText.transform.parent.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            displayText.transform.parent.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        try
        {
            Fraction displayFraction = isUpsideDown ?
                currentFraction.GetReciprocal() : currentFraction;

            displayText.text = displayFraction.ToString();
            // 可选：添加方向指示
            // string orientation = isUpsideDown ? "倒置 (显示倒数)" : "正置 (显示原数)";
            // displayText.text += $"\n\n<size=12><color=#888888>{orientation}</color></size>";
        }
        catch (Exception e)
        {
            displayText.text = $"错误: {e.Message}";
        }
    }

    // 解析输入
    private Fraction ParseInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new Fraction(1, 2); // 默认值
            
        input = input.Trim();
        
        // 检查是否是分数格式 (a/b)
        if (input.Contains("/"))
        {
            string[] parts = input.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException("分数格式不正确，请使用 a/b 格式");
            
            if (!double.TryParse(parts[0], out double numerator) || 
                !double.TryParse(parts[1], out double denominator))
                throw new ArgumentException("分数分子或分母不是有效数字");
                
            if (Math.Abs(denominator) < 0.0001)
                throw new ArgumentException("分母不能为零");
                
            return new Fraction(numerator, denominator);
        }
        
        // 解析为小数或整数
        if (!double.TryParse(input, out double value))
            throw new ArgumentException("输入不是有效数字");
            
        return new Fraction(value);
    }
}

// 分数类
[System.Serializable]
public struct Fraction
{
    public double numerator;
    public double denominator;
    
    public Fraction(double value)
    {
        numerator = value;
        denominator = 1;
        ConvertToFraction(ref numerator, ref denominator, 0.0001, 1000);
        Simplify();
    }
    
    public Fraction(double num, double den)
    {
        numerator = num;
        denominator = den;
        Simplify();
    }
    
    // 获取倒数
    public Fraction GetReciprocal()
    {
        if (Math.Abs(numerator) < 0.0001)
            throw new InvalidOperationException("零没有倒数");
            
        return new Fraction(denominator, numerator);
    }
    
    // 转换为字符串（优化显示格式）
    public override string ToString()
    {
        // 如果是整数，直接显示分子
        if (Math.Abs(denominator - 1) < 0.0001)
        {
            return FormatNumber(numerator);
        }
        
        // 检查分子分母是否为整数
        bool numIsInt = IsInteger(numerator);
        bool denIsInt = IsInteger(denominator);
        
        if (numIsInt && denIsInt)
        {
            string view_numerator = FormatNumber(numerator);
            string view_denominator = FormatNumber(denominator);
            int view_width = Math.Max(view_denominator.Length, view_numerator.Length);
            // 分子分母都是整数，显示为分数格式
            return $"{view_numerator}\n<cspace=-25>{string.Concat(Enumerable.Repeat('—', view_width))}</cspace>\n{view_denominator}";
        }
        else
        {
            // 至少有一个不是整数，显示为小数格式
            double value = numerator / denominator;
            string decimalStr = value.ToString("F6").TrimEnd('0').TrimEnd('.');
            return string.IsNullOrEmpty(decimalStr) ? "0" : decimalStr;
        }
    }
    
    // 检查是否为整数
    private bool IsInteger(double number)
    {
        return Math.Abs(number - Math.Round(number)) < 0.0001;
    }
    
    // 格式化数字显示（如果是整数则不显示小数部分）
    private string FormatNumber(double number)
    {
        if (IsInteger(number))
        {
            return Math.Round(number).ToString();
        }
        else
        {
            // 显示最多6位小数，并去除末尾的0
            string result = number.ToString("F6").TrimEnd('0').TrimEnd('.');
            return string.IsNullOrEmpty(result) ? "0" : result;
        }
    }
    
    // 简化分数
    private void Simplify()
    {
        if (Math.Abs(denominator) < 0.0001) return;
        
        // 确保分母为正
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }
        
        // 寻找最大公约数进行简化
        double gcd = GCD(Math.Abs(numerator), denominator);
        if (gcd > 0.0001)
        {
            numerator /= gcd;
            denominator /= gcd;
        }
    }
    
    // 计算最大公约数
    private double GCD(double a, double b)
    {
        // 处理整数情况
        long aInt = (long)Math.Round(a);
        long bInt = (long)Math.Round(b);
        
        if (Math.Abs(a - aInt) < 0.0001 && Math.Abs(b - bInt) < 0.0001)
        {
            // 使用整数GCD算法
            while (bInt != 0)
            {
                long temp = bInt;
                bInt = aInt % bInt;
                aInt = temp;
            }
            return aInt;
        }
        
        // 使用浮点数GCD算法
        while (Math.Abs(b) > 0.0001)
        {
            double temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
    
    // 将小数转换为近似分数
    private void ConvertToFraction(ref double numerator, ref double denominator, double precision, int maxDenominator)
    {
        if (Math.Abs(numerator) < precision)
        {
            numerator = 0;
            denominator = 1;
            return;
        }
        
        double wholePart = Math.Floor(numerator);
        double fractionalPart = numerator - wholePart;
        
        if (Math.Abs(fractionalPart) < precision)
        {
            numerator = wholePart;
            denominator = 1;
            return;
        }
        
        // 使用连分数法寻找最佳近似
        double lowerN = 0, lowerD = 1;
        double upperN = 1, upperD = 0;
        
        while (true)
        {
            double middleN = lowerN + upperN;
            double middleD = lowerD + upperD;
            
            if (middleD > maxDenominator) break;
            
            if (fractionalPart * middleD < middleN)
            {
                upperN = middleN;
                upperD = middleD;
            }
            else
            {
                lowerN = middleN;
                lowerD = middleD;
            }
            
            double approx = middleN / middleD;
            if (Math.Abs(fractionalPart - approx) < precision)
            {
                numerator = wholePart * middleD + middleN;
                denominator = middleD;
                Simplify();
                return;
            }
        }
        
        // 如果找不到精确匹配，使用最后找到的分数
        numerator = wholePart * lowerD + lowerN;
        denominator = lowerD;
        Simplify();
    }
}
