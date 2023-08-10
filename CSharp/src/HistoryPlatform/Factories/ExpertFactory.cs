using Common.Experts;
using ExpertCollection;

namespace HistoryPlatform.Factories
{
    public class ExpertFactory : IExpertFactory
    {
        public IExpert Switcher(string expertName)
        {
            IExpert newExpert = null;

            switch (expertName)
            {
                case "GoldenGooseV1":
                    newExpert = new GoldenGooseV1();
                    return newExpert;
                case "GoldenGooseV2":
                    newExpert = new GoldenGooseV2();
                    return newExpert;
                case "GoldenGooseV3":
                    newExpert = new GoldenGooseV3();
                    return newExpert;
                case "GoldenGooseV4":
                    newExpert = new GoldenGooseV4();
                    return newExpert;
                case "GoldenGooseV6":
                    newExpert = new GoldenGooseV6();
                    return newExpert;
                case "SltpExpert":
                    newExpert = new SltpExpert();
                    return newExpert;
            }

            return newExpert;
        }
    }
}
