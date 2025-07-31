using System;

namespace GAS.General
{
    /// <summary>
    /// 字符串哈希工具类
    /// 提供一致性的字符串哈希计算方法
    /// </summary>
    public static class StringHashUtil
    {
        /// <summary>
        /// 计算字符串的哈希值
        /// </summary>
        /// <param name="str">要计算哈希的字符串</param>
        /// <returns>哈希值</returns>
        public static int GetHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
                
            return str.GetHashCode();
        }
        
        /// <summary>
        /// 计算字符串的稳定哈希值（不受.NET版本影响）
        /// </summary>
        /// <param name="str">要计算哈希的字符串</param>
        /// <returns>稳定的哈希值</returns>
        public static int GetStableHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
                
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                
                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }
                
                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}