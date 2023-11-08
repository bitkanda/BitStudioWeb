using System.ComponentModel.DataAnnotations;

namespace BitStudioWeb.Models
{
    public class VerificationModel
    {
        [Display(Name ="手机号")]
        [Required(ErrorMessage = "手机号码是必填的")]
        [RegularExpression(@"^1[3456789]\d{9}$", ErrorMessage = "请输入有效的手机号码")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 验证码
        /// </summary>
        [Display(Name = "验证码")]
        [Required(ErrorMessage = "验证码是必填的")]
        public int Code { get; set; }
    }
}
