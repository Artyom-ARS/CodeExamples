//+------------------------------------------------------------------+
//|                                            LoadOrdersHistory.mq4 |
//|                                                              ACE |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "ACE"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs
//--- input parameters
input string FileName = "1.csv";
//--- local parameters
string ScriptObjectNameList[100000];
int ScriptObjectNameCount = 0;
bool ShouldNotDeleteArrows = false;
string Prefix = "LoadOrdersHistory_line";
int PriceIndent = 1;
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ReadTradesFromFile();
   
   while(IsStopped()==false)
   {
      Sleep(1000);
   }
   
   ClearAll();
}
//+------------------------------------------------------------------+
void ReadTradesFromFile()
{
   int hd;
   //int orderLot;
   //double orderOpen, orderClose;
   datetime orderDtOpen, orderDtClose;
   string orderBuySell, orderComment, orderTmpOpen, orderTmpClose, orderTmpLot, orderInstrument;
   hd = FileOpen(FileName,FILE_CSV|FILE_READ);
   if(hd<1)
   {
      string err = GetLastError();
      Print("Statement File ",FileName,"  Not found!!! Please check his!");
      Print("Error code ",err); 
      return;
   }
   long orderId = 0;
   while(!FileIsEnding(hd))
   {
      orderInstrument = FileReadString(hd);
      orderDtOpen = FileReadDatetime(hd);
      orderDtClose = FileReadDatetime(hd);
      orderTmpOpen = FileReadString(hd);
      orderTmpClose = FileReadString(hd);
      orderBuySell = FileReadString(hd);
      orderTmpLot = FileReadString(hd);
      orderComment = FileReadString(hd);
      DrawOrder(orderBuySell=="B", StrToDouble(orderTmpOpen), StrToDouble(orderTmpClose), orderDtOpen, orderDtClose, StrToInteger(orderTmpLot), orderId);
      orderId++;
   }
   FileClose(hd); 
}
//+------------------------------------------------------------------+
//void DrawOrder(bool buy, double p1, double p2, int b1, int b2, double lot, int closeMark, double spread, int count, int orderId)
void DrawOrder(bool buy, double p1, double p2, datetime t1, datetime t2, int lot, long orderId)
{
  string name1, name2, name3, name4, closeMarkText;
  double spread = 0.0;
  ENUM_OBJECT arrow;
  color arrowColor;
  //if (shouldShowLossOnly && count<LossCountForShow) return;
  name1 = StringConcatenate(Prefix,"_",IntegerToString(orderId,3,'0'),"_open");
  name2 = StringConcatenate(Prefix,"_",IntegerToString(orderId,3,'0'),"_line");
  name3 = StringConcatenate(Prefix,"_",IntegerToString(orderId,3,'0'),"_close");
  name4 = StringConcatenate(Prefix,"_",IntegerToString(orderId,3,'0'),"_vl");
  int closeMark;
  if (buy && p2>=p1) closeMark = 1;
  if (buy && p2<p1) closeMark = 0;
  if (!buy && p2<=p1) closeMark = 1;
  if (!buy && p2>p1) closeMark = 0;
  
  switch(closeMark)
    {
     case 0 :
       closeMarkText = "0-SL";
       break;
     case 1 :
       closeMarkText = "1-TP";
       break;
     default:
       closeMarkText = "End";
       break;
    }
  
  if (buy) arrow = OBJ_ARROW_BUY;
  if (buy) arrowColor = clrBlue;
  //if (buy && p2-p1<0) arrowColor = clrTeal;
  if (!buy) arrow = OBJ_ARROW_SELL;
  if (!buy) arrowColor = clrRed;
  //if (!buy && p1-p2<0) arrowColor = clrTeal;
  
  ObjectCreate(name1,arrow,0,t1,p1);  
  //ObjectCreate(name1,arrow,0,Time[b1],p1);
  ObjectSetInteger(0,name1,OBJPROP_COLOR,arrowColor);
  ObjectSetInteger(0,name1,OBJPROP_BACK,false);
  ObjectSetInteger(0,name1,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name1,OBJPROP_SELECTED,false);
  ObjectSetText(name1,StringFormat("Объем: %0.0f, Профит: %0.1f",lot,buy>0?(lot*(p2-p1-spread)/Point):(lot*(p1-p2-spread)/Point)));
  
  //ObjectCreate(0,name2,OBJ_TREND,0,Time[b1],p1,Time[b2],p2); 
  ObjectCreate(0,name2,OBJ_TREND,0,t1,p1,t2,p2); 
  ObjectSetInteger(0,name2,OBJPROP_STYLE,STYLE_DOT);
  ObjectSetInteger(0,name2,OBJPROP_RAY_LEFT,false);
  ObjectSetInteger(0,name2,OBJPROP_RAY_RIGHT,false);
  ObjectSetInteger(0,name2,OBJPROP_COLOR,arrowColor);
  ObjectSetInteger(0,name3,OBJPROP_BACK,false);
  ObjectSetInteger(0,name3,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name3,OBJPROP_SELECTED,false);
  
  //ObjectCreate(0,name3,OBJ_ARROW,0,Time[b2],p2);  
  ObjectCreate(0,name3,OBJ_ARROW,0,t2,p2);  
  ObjectSet(name3, OBJPROP_ARROWCODE, SYMBOL_RIGHTPRICE);
  ObjectSetInteger(0,name3,OBJPROP_ANCHOR,ANCHOR_BOTTOM);
  ObjectSetInteger(0,name3,OBJPROP_WIDTH,3);
  ObjectSetInteger(0,name3,OBJPROP_COLOR,arrowColor);
  ObjectSetInteger(0,name3,OBJPROP_BACK,false);
  ObjectSetInteger(0,name3,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name3,OBJPROP_SELECTED,false);
  ObjectSetText(name3,closeMarkText + StringFormat(" Объем: %0.0f, Профит: %0.1f",lot,buy>0?(lot*(p2-p1-spread)/Point):(lot*(p1-p2-spread)/Point)));
  
  /*
  ObjectCreate(name4,OBJ_VLINE,0,Time[b1],0.0);
  ObjectSetInteger(0,name4,OBJPROP_COLOR,arrowColor); 
  ObjectSetInteger(0,name4,OBJPROP_STYLE,STYLE_DASH);
  ObjectSetInteger(0,name4,OBJPROP_WIDTH,1);
  ObjectSetInteger(0,name4,OBJPROP_BACK,true);
  ObjectSetInteger(0,name4,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name4,OBJPROP_SELECTED,false);
  ObjectSetInteger(0,name4,OBJPROP_HIDDEN,false); 
  */ 
  
  ScriptObjectNameList[ScriptObjectNameCount] = name1; 
  ScriptObjectNameCount++;
  ScriptObjectNameList[ScriptObjectNameCount] = name2; 
  ScriptObjectNameCount++;
  ScriptObjectNameList[ScriptObjectNameCount] = name3; 
  ScriptObjectNameCount++;
  ScriptObjectNameList[ScriptObjectNameCount] = name4; 
  ScriptObjectNameCount++;
  return;
}
//+------------------------------------------------------------------+
void AddVLine (int i, int count)
{
  string name4 = StringConcatenate(Prefix,"_",IntegerToString(ScriptObjectNameCount),"_vl");
  ObjectCreate(name4,OBJ_VLINE,0,Time[i],0.0);
  ObjectSetInteger(0,name4,OBJPROP_COLOR,clrGold); 
  ObjectSetInteger(0,name4,OBJPROP_STYLE,STYLE_DASH);
  ObjectSetInteger(0,name4,OBJPROP_WIDTH,1);
  ObjectSetInteger(0,name4,OBJPROP_BACK,true);
  ObjectSetInteger(0,name4,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name4,OBJPROP_SELECTED,false);
  ObjectSetInteger(0,name4,OBJPROP_HIDDEN,false);  
  ScriptObjectNameList[ScriptObjectNameCount] = name4; 
  ScriptObjectNameCount++; 
  
  string name5 = StringConcatenate(Prefix,"_",IntegerToString(ScriptObjectNameCount),"_vl-mark");
  
  ObjectCreate(0,name5,OBJ_ARROW,0,Time[i],High[i]+_Point*PriceIndent*2);  
  ObjectSet(name5, OBJPROP_ARROWCODE, SYMBOL_RIGHTPRICE);
  ObjectSetInteger(0,name5,OBJPROP_ANCHOR,ANCHOR_BOTTOM);
  ObjectSetInteger(0,name5,OBJPROP_WIDTH,3);
  ObjectSetInteger(0,name5,OBJPROP_COLOR,clrGold);
  ObjectSetInteger(0,name5,OBJPROP_BACK,false);
  ObjectSetInteger(0,name5,OBJPROP_SELECTABLE,false);
  ObjectSetInteger(0,name5,OBJPROP_SELECTED,false);
  ObjectSetText(name5,"Кол-во ордеров: " + IntegerToString(count));
  ScriptObjectNameList[ScriptObjectNameCount] = name5; 
  ScriptObjectNameCount++; 
}
//+------------------------------------------------------------------+
void ClearAll()
{
  if (!ShouldNotDeleteArrows)
  {
    for (int i=0;i<ScriptObjectNameCount;i++)
    {
      if (ObjectFind(ScriptObjectNameList[i])!=-1)
      {
       ObjectDelete(ScriptObjectNameList[i]);
      }
    }      
    Comment(" ");
  }
}
//+------------------------------------------------------------------+
