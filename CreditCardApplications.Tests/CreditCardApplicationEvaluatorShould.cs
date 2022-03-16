using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using Xunit;

namespace CreditCardApplications.Tests
{
    public class CreditCardApplicationEvaluatorShould
    {
        private Mock<IFrequentFlyerNumberValidator> mockValidator;
        private CreditCardApplicationEvaluator sut;

        public CreditCardApplicationEvaluatorShould()
        {
            mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.SetupAllProperties();
            //mockValidator.DefaultValue = DefaultValue.Mock;
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);
            
            sut = new CreditCardApplicationEvaluator(mockValidator.Object);
        }

        [Fact]
        public void AcceptHighIncomeApplications()
        {
            var application = new CreditCardApplication { GrossAnnualIncome = 100_000 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoAccepted, decision);
        }

        [Fact]
        public void ReferYoungApplications()
        {
            mockValidator.DefaultValue = DefaultValue.Mock;  // Remove null default values of a propertie. Reusable setup

            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);

            var application = new CreditCardApplication { Age = 19 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void DeclineLowIncomeApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            mockValidator.Setup(x => x.IsValid(It.IsRegex("[a-z]")))
                         .Returns(true);  

            var application = new CreditCardApplication { 
                GrossAnnualIncome = 19_999,
                Age = 42,
                FrequentFlyerNumber = "x"
            };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);

        }

        // on Strict behavior mode, we must explicity the setup for Mock object for all situations.

        [Fact]
        public void ReferInvalidFrequenctFlyerApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(false);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void DeclineLowIncomeApplicationsOutDemo()
        {
            bool isValid = true;
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>(), out isValid));

            var application = new CreditCardApplication{ 
                GrossAnnualIncome = 19_999,
                Age = 42
            };

            CreditCardApplicationDecision decision = sut.EvaluateUsingOut(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferWhenLicenseKeyExpired()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("EXPIRED");

            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);


            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);

            var application = new CreditCardApplication { Age = 42 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        string GetLicenseKeyExpiryString()
        {
            // E.g. read from vendor-supplied constants file
            return "EXPIRED";
        }

        [Fact]
        public void UserDetailedLookupForOlderApplications()
        {
            var application = new CreditCardApplication { Age = 30 };

            sut.Evaluate(application);

            Assert.Equal(ValidationMode.Detailed, mockValidator.Object.ValidationMode);

        }

        [Fact]
        public void ValidateFrequentFlyerNumberForLowIncomeApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            var application = new CreditCardApplication { 
                FrequentFlyerNumber = "q" 
            };

            sut.Evaluate(application);

            mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotValidateFrequentFlyerNumberForLowIncomeApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("EXPIRED");

            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 100_000
            };

            sut.Evaluate(application);

            mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), Times.Never);
        }

        
        [Fact]
        public void CheckLicenseKeyForLowIncomeApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 90_000
            };

            sut.Evaluate(application);

            //verifying a property value has been accessed in the process
            mockValidator.VerifyGet(x => x.ServiceInformation.License.LicenseKey);

        }

        [Fact]
        public void SetDetailedLookupForOlderApplications()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            var application = new CreditCardApplication { Age = 30 };

            sut.Evaluate(application);

            mockValidator.VerifySet(x => x.ValidationMode = It.IsAny<ValidationMode>(), Times.Once);
        }

        [Fact]
        public void ReferWhenFrequentFlyerValidationError()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws<Exception>();
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws(new Exception("Custom message"));

            var application = new CreditCardApplication { Age = 42 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void IncrementLookupCount()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>()))
                .Returns(true)
                .Raises(x => x.ValidatorLookupPerformed += null, EventArgs.Empty);  // Mock event automatically

            var application = new CreditCardApplication { FrequentFlyerNumber = "x", Age = 25 };

            sut.Evaluate(application);

            // Manualy
            // mockValidator.Raise(x => x.ValidatorLookupPerformed += null, EventArgs.Empty);

            Assert.Equal(1, sut.ValidatorLookupCount);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplications_ReturnValuesSequence()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            
            mockValidator.SetupSequence(x => x.IsValid(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);

            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision firstDecision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, firstDecision);

            CreditCardApplicationDecision secondDecision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, secondDecision);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplications_MultipleCallsSequence()
        {
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");

            var frequentFlyerNumbersPassed = new List<string>();
            mockValidator.Setup(x => x.IsValid(Capture.In(frequentFlyerNumbersPassed)));

            var application1 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "aa" };
            var application2 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "bb" };
            var application3 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "cc" };

            sut.Evaluate(application1);
            sut.Evaluate(application2);
            sut.Evaluate(application3);

            // Assert that IsValid was called three times with "aa", "bb", and "cc" values
            Assert.Equal(new List<string> { "aa", "bb", "cc" }, frequentFlyerNumbersPassed);
        }


        // Example of partial mock. Using "virtual"
        [Fact]
        public void ReferFraudRisk()
        {
            Mock<FraudLookup> mockFraudLookup = new Mock<FraudLookup>();
            
            mockFraudLookup.Protected()
                           .Setup<bool>("CheckApplication", ItExpr.IsAny<CreditCardApplication>())
                           .Returns(true);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object, mockFraudLookup.Object);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            
            Assert.Equal(CreditCardApplicationDecision.ReferredHumanFraudRisk, decision);
        }

        [Fact]
        public void LinqToMocks()
        {
            IFrequentFlyerNumberValidator mockValidator = Mock.Of<IFrequentFlyerNumberValidator>(
                validator => validator.ServiceInformation.License.LicenseKey == "OK" &&
                             validator.IsValid(It.IsAny<string>()) == true
                );

            // Override sut in class field to demonstrate LINQ Setup
            var sut = new CreditCardApplicationEvaluator(mockValidator);

            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }
    }
}
