using Microsoft.AspNetCore.Builder;

namespace TradeImportsDecisionDeriver.Test.Config;

public class EnvironmentTest
{

   [Fact]
   public void IsNotDevModeByDefault()
   { 
       var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
       var isDev = TradeImportsDecisionDeriver.Config.Environment.IsDevMode(builder);
       Assert.False(isDev);
   }
}
