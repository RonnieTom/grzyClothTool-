using System;
using grzyClothTool.Helpers;

namespace grzyClothTool.Models.Texture;
#nullable enable

public class GTextureDetails
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MipMapCount { get; set; }
    public string Compression { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? Type { get; set; } = string.Empty;

    public bool IsOptimizeNeeded { get; set; }
    public string IsOptimizeNeededTooltip { get; set; } = string.Empty;

    public void Validate()
    {
        IsOptimizeNeeded = false;
        IsOptimizeNeededTooltip = string.Empty;
        
        int resolutionLimit = 2048;
        
        if (Type != null)
        {
            if (Type.Contains("diffuse", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitDiffuse;
            }
            else if (Type.Contains("normal", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitNormal;
            }
            else if (Type.Contains("specular", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitSpecular;
            }
        }
        
        if (Width > resolutionLimit || Height > resolutionLimit)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += $"纹理分辨率：{Width}x{Height}，超出您设置的限制（{resolutionLimit}）。请优化以减小体积。\n";
        }

        if ((Height & Height - 1) != 0 || (Width & Width - 1) != 0)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "纹理高度或宽度不是 2 的幂。请优化以修复此问题。\n";
        }

        var expectedMipMapCount = ImgHelper.GetCorrectMipMapAmount(Width, Height);
        if (MipMapCount == 1 && MipMapCount != expectedMipMapCount)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += $"纹理有 {MipMapCount} 级 Mipmap，应为 {expectedMipMapCount} 级。请优化以生成正确数量。\n";
        }
    }
}
