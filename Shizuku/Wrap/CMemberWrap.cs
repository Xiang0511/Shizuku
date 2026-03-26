using Shizuku.Models;
using System.ComponentModel;

namespace Shizuku.Wrap
{
    public class CMemberWrap
    {
        private TMember _prod;

        public TMember member
        {
            get { return _prod; }
            set { _prod = value; }
        }

        public CMemberWrap()
        {
            _prod = new TMember();
        }

        public int FId
        {
            get { return _prod.FId; }
            set { _prod.FId = value; }
        }

        [DisplayName("會員編號")]
        public string? FMemberId
        {
            get { return _prod.FMemberId; }
            set { _prod.FMemberId = value; }
        }

        [DisplayName("Email帳號")]
        public string? FAccount
        {
            get { return _prod.FAccount; }
            set { _prod.FAccount = value; }
        }

        [DisplayName("密碼")]
        public string? FPassword
        {
            get { return _prod.FPassword; }
            set { _prod.FPassword = value; }
        }

        [DisplayName("會員名稱")]
        public string? FName
        {
            get { return _prod.FName; }
            set { _prod.FName = value; }
        }

        [DisplayName("Email帳號")]
        public string? FEmail
        {
            get { return _prod.FEmail; }
            set { _prod.FEmail = value; }
        }

        [DisplayName("電話號碼")]
        public string? FPhone
        {
            get { return _prod.FPhone; }
            set { _prod.FPhone = value; }
        }


        public DateOnly? FBirthday
        {
            get { return _prod.FBirthday; }
            set { _prod.FBirthday = value; }
        }

        public int? FGender
        {
            get { return _prod.FGender; }
            set { _prod.FGender = value; }
        }

        [DisplayName("會員等級")]
        public int? FLevel
        {
            get { return _prod.FLevel; }
            set { _prod.FLevel = value; }
        }

        public DateTime? FCreatedTime
        {
            get { return _prod.FCreatedTime; }
            set { _prod.FCreatedTime = value; }
        }

        public DateTime? FUpdatedTime
        {
            get { return _prod.FUpdatedTime; }
            set { _prod.FUpdatedTime = value; }
        }

        public bool? FIsActive
        {
            get { return _prod.FIsActive; }
            set { _prod.FIsActive = value; }
        }
        public string? FReceiverName
        {
            get { return _prod.FReceiverName; }
            set { _prod.FReceiverName = value; }
        }

        public string? FReceiverPhone
        {
            get { return _prod.FReceiverPhone; }
            set { _prod.FReceiverPhone = value; }
        }

        public string? FReceiverAddress
        {
            get { return _prod.FReceiverAddress; }
            set { _prod.FReceiverAddress = value; }
        }


        [DisplayName("登入時間")]
        public DateTime? FLoginTime
        {
            get { return _prod.FLoginTime; }
            set { _prod.FLoginTime = value; }
        }

        public string? FIpAddress
        {
            get { return _prod.FIpAddress; }
            set { _prod.FIpAddress = value; }
        }

        public string? FWishlist
        {
            get { return _prod.FWishlist; }
            set { _prod.FWishlist = value; }
        }

        public string? FImage
        {
            get { return _prod.FImage; }
            set { _prod.FImage = value; }
        }
    }
}

