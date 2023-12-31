//+------------------------------------------------------------------+
//|                                                  tmm-ep-only.mq4 |
//|                                      Copyright © 2023, artem-ace |
//|                                             https://xperience.lv |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2023, artem-ace"
#property link      "https://www.xperience.lv"
#property version   "1.00"
#property strict

#include <stderror.mqh> 
#include <stdlib.mqh> 

const string sButton1Name = "tmm-button-buy";
const string sButton2Name = "tmm-button-sell";
const string sButton3Name = "tmm-button-stop";
const string sText1Name = "tmm-alarm-text";
const string sSymbolsFile = "tmm-symbols.csv";
const string sLevelsFile = "day-levels.csv";
const string sAlarmTextNotActive = "ROBOT IS NOT ACTIVE! CLICK BUY OR SELL BUTTON!";
const string sAlarmTextActiveBuy = "ROBOT IS ACTIVE! IT'S TRACKING BUY!";
const string sAlarmTextActiveSell = "ROBOT IS ACTIVE! IT'S TRACKING SELL!";

struct SymbolData
{
   double lots;
   double stopLoss;
   string symbol;
};
enum OrderTypeCustom 
{ 
   OTC_stop,
   OTC_limit,
   OTC_market
};
enum TrackingState 
{
   TS_none,
   TS_state1,
   TS_state2,
   TS_state3,
   TS_act
};
enum TrackingPattern
{
   TP_none,
   TP_limit,
   TP_retest
};

SymbolData symbolData;
double levelBidPrice;
TrackingState trackingState;
TrackingPattern trackingPattern;
datetime patternStartTime;
double patternLowestPrice;
double patternHighestPrice;
int orderType = OP_SELLSTOP;

int OnInit()
{
   
   CreateBitmapButton(sButton1Name, "\\Images\\b5.bmp", 100, 20);
   CreateBitmapButton(sButton2Name, "\\Images\\b6.bmp", 150, 20);
   CreateBitmapButton(sButton3Name, "\\Images\\b7.bmp", 200, 20);
   CreateAlarmText(sText1Name, sAlarmTextNotActive, 300, 40, clrGray);
   
   symbolData = LoadDataFromFile(Symbol());

   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
   ObjectDelete(sButton1Name);
   ObjectDelete(sButton2Name);
   ObjectDelete(sButton3Name);
   ObjectDelete(sText1Name);
}

void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
{
   if (id == CHARTEVENT_OBJECT_CLICK && sparam==sButton1Name)
   {
      Print("Bitmap label 1 click");
      StartBuyOrder();
   }
   if (id == CHARTEVENT_OBJECT_CLICK && sparam==sButton2Name)
   {
      Print("Bitmap label 2 click");
      StartSellOrder();
   }
   if (id == CHARTEVENT_OBJECT_CLICK && sparam==sButton3Name)
   {
      Print("Bitmap label 3 click");
      StopTracking();
   }

}

void OnTick()
{
   // Only needed in Visual Testing Mode
   if( IsVisualMode() )
   {
      // Check Chart Buttons in Visual Mode
      CheckButtons();
   }
   
   TrackPrice();

   return;
}

void CheckButtons()
{
   if( bool( ObjectGetInteger( 0, sButton2Name, OBJPROP_STATE ) ) )
   {
      Print( "Sell Button Clicked" );
      ObjectSetInteger( 0, sButton2Name, OBJPROP_STATE, false );
      StartSellOrder();
   }
   if( bool( ObjectGetInteger( 0, sButton1Name, OBJPROP_STATE ) ) )
   {
      Print( "Buy Button Clicked" );
      ObjectSetInteger( 0, sButton1Name, OBJPROP_STATE, false );
      StartBuyOrder();
   }
   if( bool( ObjectGetInteger( 0, sButton3Name, OBJPROP_STATE ) ) )
   {
      Print( "Stop Button Clicked" );
      ObjectSetInteger( 0, sButton3Name, OBJPROP_STATE, false );
      StopTracking();
   }
}

void CreateBitmapButton(string sName, string file, int xDistance, int yDistance)
{
   if (ObjectFind(sName)< 0)
   {
      ObjectCreate(0,sName,OBJ_BITMAP_LABEL,0,0,0);
   }

   ObjectSetInteger(0,sName,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetInteger(0,sName,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,sName,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,sName,OBJPROP_XSIZE,40);
   ObjectSetInteger(0,sName,OBJPROP_YSIZE,40);

   if (!ObjectSetString(0,sName,OBJPROP_BMPFILE,0,file))
   {
      Print(__FUNCTION__,
            ": failed to load the image for On mode! Error code = ",GetLastError());
   }
   if (!ObjectSetString(0,sName,OBJPROP_BMPFILE,1,file))
   {
      Print(__FUNCTION__,
            ": failed to load the image for Off mode! Error code = ",GetLastError());
   }
     
   ObjectSetInteger(0, sName,OBJPROP_HIDDEN,false);
   ObjectSetInteger(0, sName,OBJPROP_SELECTABLE,false);
}

void CreateArrow(datetime time, double price, int arrow_code, int clr, string prefix="tmm_")
{
   string sName = prefix+IntegerToString(rand(),5,'0');
   if (ObjectFind(sName)< 0)
   {
      ObjectCreate(0,sName,OBJ_ARROW,0,time,price);
   }

   ObjectSetInteger(0,sName,OBJPROP_ARROWCODE,arrow_code);
   ObjectSetInteger(0,sName,OBJPROP_COLOR,clr);
   
   ObjectSetInteger(0, sName,OBJPROP_ANCHOR, ANCHOR_BOTTOM); 
   ObjectSetInteger(0, sName,OBJPROP_HIDDEN,false);
   // ObjectSetInteger(0, sName,OBJPROP_SELECTABLE,false);

}

void CreateAlarmText(string sName, string sText, int xDistance, int yDistance, int clr)
{
   if (ObjectFind(sName)< 0)
   {
      ObjectCreate(0,sName,OBJ_LABEL,0,0,0);
   }

   ObjectSetInteger(0, sName,OBJPROP_ANCHOR, ANCHOR_BOTTOM); 
   ObjectSetInteger(0, sName,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetInteger(0, sName,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0, sName,OBJPROP_YDISTANCE,yDistance);
   
   ObjectSetString(0, sName,OBJPROP_TEXT,sText); 
   
   ObjectSetInteger(0, sName,OBJPROP_COLOR,clr);
   ObjectSetInteger(0, sName,OBJPROP_FONTSIZE,20); 
   
   ObjectSetInteger(0, sName,OBJPROP_HIDDEN,false);
   ObjectSetInteger(0, sName,OBJPROP_SELECTABLE,false);

}

void StartBuyOrder()
{
   string symbol = Symbol();
   double bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   int symbolDigits=(int)MarketInfo(symbol,MODE_DIGITS);
   
   levelBidPrice = GetClosestPriceLevel(bid);
   levelBidPrice = NormalizeDouble(levelBidPrice,symbolDigits);
   if (levelBidPrice > 0.0)
   {
      orderType = OP_BUY;
      CreateAlarmText(sText1Name, sAlarmTextActiveBuy, 300, 40, clrLightBlue);
      Print(__FUNCTION__, ": levelBidPrice = ",levelBidPrice);
      return;
   }
}

void StartSellOrder()
{
   string symbol = Symbol();
   double bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   int symbolDigits=(int)MarketInfo(symbol,MODE_DIGITS);
   
   levelBidPrice = GetClosestPriceLevel(bid);
   levelBidPrice = NormalizeDouble(levelBidPrice,symbolDigits);
   if (levelBidPrice > 0.0)
   {
      orderType = OP_SELL;
      CreateAlarmText(sText1Name, sAlarmTextActiveSell, 300, 40, clrLightPink);
      Print(__FUNCTION__, ": levelBidPrice = ",levelBidPrice);
      return;
   }
}

void StopTracking()
{
   orderType = OP_SELLSTOP;
   trackingState = TS_none;
   trackingPattern = TP_none;
   // CreateArrow(Time[0], High[0], 3, clrGray);
   CreateAlarmText(sText1Name, sAlarmTextNotActive, 300, 40, clrGray);
   Print(__FUNCTION__, ": State = ",trackingState);
}

void TrackPrice()
{
   string symbol = Symbol();
   double stopLoss = 0.0;
   stopLoss = GetStopLossBySymbol(symbol);
   double priceSpan = NormalizeDouble(10*Point,Digits);
   double spread = Ask-Bid;
   int timeFrame = ChartPeriod();
   
   // Entry Point Limit
   // Entry Point Retest
   if(orderType == OP_SELL && levelBidPrice > 0.0)
   {
      if (IdentifiedSellByLimitPattern() || IdentifiedSellByRetestPattern())
      {
         OpenSellOrder(OTC_market);
      }
   }
      
   if(orderType == OP_BUY && levelBidPrice > 0.0)
   {
      if (IdentifiedBuyByLimitPattern() || IdentifiedBuyByRetestPattern())
      {
         OpenBuyOrder(OTC_market);
      }
   }

}

bool IdentifiedBuyByRetestPattern()
{
   string symbol = Symbol();
   double stopLoss = 0.0;
   stopLoss = GetStopLossBySymbol(symbol);
   double priceSpan = NormalizeDouble(10*Point,Digits);
   double spread = Ask-Bid;
   int timeFrame = ChartPeriod();
   
   if (trackingState == TS_none && Bid>levelBidPrice)
   {
      double lowestLowPrice = 0.0;
      int low_index=iLowest(symbol,timeFrame,MODE_LOW,5,1);
      // Print(__FUNCTION__, ": Pattern =  Entry Point Retest, Lowest index = ", low_index);
      
      if (low_index>1 && low_index<5)
      {
         lowestLowPrice=Low[low_index];
      }
      
      // Print(__FUNCTION__, ": Pattern =  Entry Point Retest, Lowest low price = ", lowestLowPrice);
      // Print(__FUNCTION__, ": Pattern =  Entry Point Retest, Level Bid Price + stopLoss*0.5 = ", levelBidPrice+stopLoss*0.5);
      // Print(__FUNCTION__, ": Pattern =  Entry Point Retest, Level Bid Price - priceSpan = ", levelBidPrice-priceSpan);
      if (lowestLowPrice > 0.0 && lowestLowPrice<levelBidPrice+stopLoss*0.5 && lowestLowPrice>levelBidPrice-priceSpan)
      {
         trackingState = TS_state1;
         trackingPattern = TP_retest;
         patternStartTime = Time[low_index];
         patternLowestPrice = lowestLowPrice;
         Print(__FUNCTION__, ": Pattern =  Entry Point Retest, State = Start");
         CreateArrow(Time[0], patternLowestPrice, 3, clrGreen, "Entry pattern: Retest, State: Start. ");
         return false;
      }
   }
   
   if(trackingState == TS_state1 && trackingPattern == TP_retest)
   {
      int barshift = iBarShift(symbol, ChartPeriod(), patternStartTime);
      if (barshift > 12 || Bid>levelBidPrice+stopLoss*2.5)
      {
        trackingState = TS_none;
        trackingPattern = TP_none;
        Print(__FUNCTION__, ": State = ", trackingState);
        CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Retest, State: None. ");
        return false;
      }
      
      if(Bid < patternLowestPrice+priceSpan)
      {
         trackingState = TS_act;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrDarkGreen, "Entry pattern: Retest, State: Act. ");
         return false;
      }
   }
   
   if(trackingState == TS_act && trackingPattern == TP_retest)
   {
      stopLoss = GetStopLossBySymbol(symbol);
      if(levelBidPrice > Bid - stopLoss + spread)
      {
         return true;
      }
      return false;
   }

   return false;
}

bool IdentifiedSellByRetestPattern()
{
   string symbol = Symbol();
   double stopLoss = 0.0;
   stopLoss = GetStopLossBySymbol(symbol);
   double priceSpan = NormalizeDouble(10*Point,Digits);
   double spread = Ask-Bid;
   int timeFrame = ChartPeriod();
   
   if (trackingState == TS_none && Bid<levelBidPrice)
   {
      double highestHighPrice = 0.0;
      int high_index=iHighest(symbol,timeFrame,MODE_HIGH,5,1);
      
      if (high_index>1 && high_index<5)
      {
         highestHighPrice=High[high_index];
      }
      
      if (highestHighPrice > 0.0 && highestHighPrice>levelBidPrice-stopLoss*0.5 && highestHighPrice<levelBidPrice+priceSpan)
      {
         trackingState = TS_state1;
         trackingPattern = TP_retest;
         patternStartTime = Time[high_index];
         patternHighestPrice = highestHighPrice;
         Print(__FUNCTION__, ": Pattern =  Entry Point Retest, State = Start");
         CreateArrow(Time[0], patternHighestPrice, 3, clrGreen, "Entry pattern: Retest, State: Start. ");
         return false;
      }
   }
   
   if(trackingState == TS_state1 && trackingPattern == TP_retest)
   {
      int barshift = iBarShift(symbol, ChartPeriod(), patternStartTime);
      if (barshift > 12 || Bid<levelBidPrice-stopLoss*2.5)
      {
        trackingState = TS_none;
        trackingPattern = TP_none;
        Print(__FUNCTION__, ": State = ", trackingState);
        CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Retest, State: None. ");
        return false;
      }
      
      if(Bid > patternLowestPrice-priceSpan)
      {
         trackingState = TS_act;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrDarkGreen, "Entry pattern: Retest, State: Act. ");
         return false;
      }
   }
   
   if(trackingState == TS_act && trackingPattern == TP_retest)
   {
      stopLoss = GetStopLossBySymbol(symbol);
      if(levelBidPrice < Bid + stopLoss - spread)
      {
         return true;
      }
      return false;
   }

   return false;
}

bool IdentifiedBuyByLimitPattern()
{
   string symbol = Symbol();
   double stopLoss = 0.0;
   stopLoss = GetStopLossBySymbol(symbol);
   double priceSpan = NormalizeDouble(10*Point,Digits);
   double spread = Ask-Bid;
   int timeFrame = ChartPeriod();
   
   if (trackingState == TS_none && Close[1]>levelBidPrice && Open[1]<levelBidPrice)
   {
      trackingState = TS_state1;
      trackingPattern = TP_limit;
      patternStartTime = Time[1];
      Print(__FUNCTION__, ": Pattern =  Entry Point Limit, State = Start");
      CreateArrow(patternStartTime, High[1]+stopLoss*0.2, 3, clrGreen, "Entry pattern: Limit, State: Start. ");
      return false;
   }
   
   if(trackingState == TS_state1 && trackingPattern == TP_limit)
   {
      int barshift = iBarShift(symbol, timeFrame, patternStartTime);
      if (barshift > 5)
      {
         trackingState = TS_none;
         trackingPattern = TP_none;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
         return false;
      }
      if (Close[1] < levelBidPrice - priceSpan)
      {
         trackingState = TS_none;
         trackingPattern = TP_none;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
         return false;
      }
      if(levelBidPrice < Low[1]
         && ((Low[1]>Low[2] && priceSpan > Low[1]-Low[2]) || (Low[2]>Low[1] && priceSpan > Low[2]-Low[1])))
      {
         if(levelBidPrice > Low[1] - stopLoss + spread)
         {
            trackingState = TS_act;
            int low_index=iLowest(symbol,timeFrame,MODE_LOW,2,1);
            if (low_index>0)
            {
               patternLowestPrice=Low[low_index];
            }
            Print(__FUNCTION__, ": State = ",trackingState);
            CreateArrow(Time[1], patternLowestPrice, 3, clrDarkGreen, "Entry pattern: Limit, State: Act. ");
            return false;
         }
         else 
         {
            trackingState = TS_none;
            trackingPattern = TP_none;
            Print(__FUNCTION__, ": State = ",trackingState);
            CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
            return false;
         }
      }
   }
   
   if(trackingState == TS_act && trackingPattern == TP_limit)
   {
      stopLoss = GetStopLossBySymbol(symbol);
      if(Bid < patternLowestPrice+priceSpan)
      {
         return true;
      }
      return false;
   }
   
   return false;
}

bool IdentifiedSellByLimitPattern()
{
   string symbol = Symbol();
   double stopLoss = 0.0;
   stopLoss = GetStopLossBySymbol(symbol);
   double priceSpan = NormalizeDouble(10*Point,Digits);
   double spread = Ask-Bid;
   int timeFrame = ChartPeriod();
   
   if (trackingState == TS_none && Close[1]<levelBidPrice && Open[1]>levelBidPrice)
   {
      trackingState = TS_state1;
      trackingPattern = TP_limit;
      patternStartTime = Time[1];
      Print(__FUNCTION__, ": Pattern =  Entry Point Limit, State = Start");
      CreateArrow(patternStartTime, High[1]+stopLoss*0.2, 3, clrGreen, "Entry pattern: Limit, State: Start. ");
      return false;
   }
   
   if(trackingState == TS_state1 && trackingPattern == TP_limit)
   {
      int barshift = iBarShift(symbol, timeFrame, patternStartTime);
      
      if (barshift > 5)
      {
         trackingState = TS_none;
         trackingPattern = TP_none;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
         return false;
      }
      
      if (Close[1] > levelBidPrice + priceSpan)
      {
         trackingState = TS_none;
         trackingPattern = TP_none;
         Print(__FUNCTION__, ": State = ",trackingState);
         CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
         return false;
      }
      
      if(levelBidPrice > High[1]
         && ((High[1]>High[2] && priceSpan > High[1]-High[2]) || (High[2]>High[1] && priceSpan > High[2]-High[1])))
      {
         if(levelBidPrice < High[1] + stopLoss - spread)
         {
            trackingState = TS_act;
            int high_index=iHighest(symbol,timeFrame,MODE_HIGH,2,1);
            if (high_index>0)
            {
               patternHighestPrice=High[high_index];
            }
            Print(__FUNCTION__, ": State = ",trackingState);
            CreateArrow(Time[1], patternHighestPrice, 3, clrDarkGreen, "Entry pattern: Limit, State: Act. ");
            return false;
         }
         else 
         {
            trackingState = TS_none;
            trackingPattern = TP_none;
            Print(__FUNCTION__, ": State = ",trackingState);
            CreateArrow(Time[0], High[0]+stopLoss*0.2, 3, clrGray, "Entry pattern: Limit, State: None. ");
            return false;
         }
      }
   }
   
   if(trackingState == TS_act && trackingPattern == TP_limit)
   {
      if(Bid > High[1]-priceSpan)
      {
         return true;
      }
      
      return false;
   }
   
   return false;
}

void OpenBuyOrder(OrderTypeCustom orderTypeCustom)
{
   string symbol = Symbol();
   double ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   double bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   int symbolDigits=(int)MarketInfo(symbol,MODE_DIGITS);
   double minLots = MarketInfo(symbol,MODE_MINLOT);
   double lotStep = MarketInfo(symbol,MODE_LOTSTEP);
   int spread = MarketInfo(symbol,MODE_SPREAD);
   double openPrice;
   int localOrderType;
   
   if(orderTypeCustom == OTC_market)
   {
      openPrice = ask;
      localOrderType = OP_BUY;
   }
   double lots = GetLotBySymbol(symbol);
   double lotsFirstPart = GetLotFirstPart(lots, minLots, lotStep);
   double lotsSecondPart = GetLotSecondPart(lots, minLots, lotStep);
   double stopLoss = GetStopLossBySymbol(symbol);
   
   int ticket=OrderSend(symbol,localOrderType,lotsFirstPart,openPrice,spread,openPrice-stopLoss,openPrice+2.0*stopLoss);
   ticket=OrderSend(symbol,localOrderType,lotsSecondPart,openPrice,spread,openPrice-stopLoss,openPrice+5.0*stopLoss);
   
   int check=GetLastError(); 
   if(check==ERR_NO_ERROR)
   {
      StopTracking();
   }
   else
   {
      Print(__FUNCTION__, "Error opening BUY order. Error: ",ErrorDescription(check));
   }
      
   return;
}

void OpenSellOrder(OrderTypeCustom orderTypeCustom)
{
   string symbol = Symbol();
   double ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   double bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   int symbolDigits=(int)MarketInfo(symbol,MODE_DIGITS);
   double minLots = MarketInfo(symbol,MODE_MINLOT);
   double lotStep = MarketInfo(symbol,MODE_LOTSTEP);
   int spread = MarketInfo(symbol,MODE_SPREAD);
   double openPrice;
   int localOrderType;
   if(orderTypeCustom == OTC_market)
   {
      openPrice = bid;
      localOrderType = OP_SELL;
   }
   double lots = GetLotBySymbol(symbol);
   double lotsFirstPart = GetLotFirstPart(lots, minLots, lotStep);
   double lotsSecondPart = GetLotSecondPart(lots, minLots, lotStep);
   double stopLoss = GetStopLossBySymbol(symbol);
   
   int ticket=OrderSend(symbol,localOrderType,lotsFirstPart,openPrice,spread,openPrice+stopLoss,openPrice-2.0*stopLoss);
   ticket=OrderSend(symbol,localOrderType,lotsSecondPart,openPrice,spread,openPrice+stopLoss,openPrice-5.0*stopLoss);
   
   int check=GetLastError(); 
   if(check==ERR_NO_ERROR)
   {
      StopTracking();
   }
   else
   {
      Print(__FUNCTION__, "Error opening SELL order. Error: ",ErrorDescription(check));
   }
      
   return;
}

double GetClosestUpPriceLevel(double bid)
{
   double price = 0.0;
   for(int i = ObjectsTotal()-1; i>=0; i--)
   {
      string objectName = ObjectName(i);
      if(ObjectType(objectName) == OBJ_HLINE)
      {
         double linePrice = ObjectGetDouble(0,objectName,OBJPROP_PRICE);
         if (linePrice > bid && (price == 0.0 || linePrice < price))
         {
            price = linePrice;
         }
      }
   }
   return price;
}

double GetClosestDownPriceLevel(double bid)
{
   double price = 0.0;
   for(int i = ObjectsTotal()-1; i>=0; i--)
   {
      string objectName = ObjectName(i);
      if(ObjectType(objectName) == OBJ_HLINE)
      {
         double linePrice = ObjectGetDouble(0,objectName,OBJPROP_PRICE);
         if (linePrice < bid && linePrice > price)
         {
            price = linePrice;
         }
      }
   }
   return price;
}

double GetClosestPriceLevel(double bid)
{
   double price = 0.0;
   double priceDiff = 9999.0;
   for(int i = ObjectsTotal()-1; i>=0; i--)
   {
      string objectName = ObjectName(i);
      if(ObjectType(objectName) == OBJ_HLINE)
      {
         double linePrice = ObjectGetDouble(0,objectName,OBJPROP_PRICE);
         if (linePrice > bid && (price == 0.0 || linePrice-bid < priceDiff))
         {
            price = linePrice;
            priceDiff = linePrice-bid;
         }
         if (linePrice < bid && bid-linePrice < priceDiff)
         {
            price = linePrice;
            priceDiff = bid-linePrice;
         }
      }
   }
   return price;
}

double GetStopLossBySymbol(string symbol)
{
   return symbolData.stopLoss;
}

double GetLotBySymbol(string symbol)
{
   return symbolData.lots;
}

double GetLotFirstPart(double lots, double minLots, double lotStep)
{
   double resultLots = lots * 0.5;
   
   int amountOfLotSteps = (int) (resultLots/lotStep);
   resultLots = lotStep*amountOfLotSteps;
   
   if(resultLots<minLots)
   {
      resultLots = minLots;
   }
   return resultLots;
}

double GetLotSecondPart(double lots, double minLots, double lotStep)
{
   double resultLots = lots - GetLotFirstPart(lots, minLots, lotStep);
   if(resultLots<minLots)
   {
      resultLots = minLots;
   }
   return resultLots;
}

SymbolData LoadDataFromFile(string symbol)
{
   SymbolData tmpSymbolData;
   int filehandle=FileOpen(sSymbolsFile,FILE_READ|FILE_CSV);
   if(filehandle!=INVALID_HANDLE)
   {
      while(!FileIsEnding(filehandle))
      {
         tmpSymbolData.symbol = FileReadString(filehandle);
         tmpSymbolData.lots = FileReadNumber(filehandle);
         tmpSymbolData.stopLoss = FileReadNumber(filehandle);
         if(tmpSymbolData.symbol == symbol)
         {
            break;
         }
      }
      FileClose(filehandle);
      Print("FileOpen OK");
   }
   else {
      Print("Operation FileOpen failed, error ",GetLastError());
         tmpSymbolData.symbol = symbol;
         tmpSymbolData.lots = MarketInfo(symbol,MODE_MINLOT)*2.0;
         tmpSymbolData.stopLoss = (SymbolInfoDouble(symbol,SYMBOL_ASK)-SymbolInfoDouble(symbol,SYMBOL_BID))*4.0;
   }
   Print("Symbol: ",tmpSymbolData.symbol," Lots: ",tmpSymbolData.lots," SL: ",tmpSymbolData.stopLoss);
   return tmpSymbolData;
}