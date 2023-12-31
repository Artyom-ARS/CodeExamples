//+------------------------------------------------------------------+
//|                                             HideChartBorders.mq4 |
//|                                      Copyright © 2022, artem-ace |
//|                                         https://www.xperience.lv |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2022, artem-ace"
#property link      "https://www.xperience.lv"
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#import "user32.dll"
  int SetWindowLongA(int hWnd,int nIndex, int dwNewLong);
  int GetWindowLongA(int hWnd,int nIndex);
  int SetWindowPos(int hWnd, int hWndInsertAfter,int X, int Y, int cx, int cy, int uFlags);
  int GetParent(int hWnd);
#import

#define GWL_STYLE         -16 
#define WS_CAPTION        0x00C00000 
#define WS_BORDER         0x00800000
#define WS_SIZEBOX        0x00040000
#define WS_DLGFRAME       0x00400000
#define SWP_NOSIZE        0x0001
#define SWP_NOMOVE        0x0002
#define SWP_NOZORDER      0x0004
#define SWP_NOACTIVATE    0x0010
#define SWP_FRAMECHANGED  0x0020

//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int start() {
   long currChart,prevChart=ChartFirst();
   int i=0,limit=100;
   Print("ChartFirst =",ChartSymbol(currChart)," ",ChartPeriod(currChart)," ID =",prevChart);
   while(i<limit)
   {
      Print(i,ChartSymbol(currChart)," ",ChartPeriod(currChart)," ID =",currChart);
      int iChartParent=GetParent(WindowHandle(ChartSymbol(currChart),ChartPeriod(currChart)));    
      int iNewStyle = GetWindowLongA(iChartParent, GWL_STYLE) & (~(WS_BORDER | WS_DLGFRAME | WS_SIZEBOX));    
      if (iChartParent>0 && iNewStyle>0) {
         SetWindowLongA(iChartParent, GWL_STYLE, iNewStyle);
         SetWindowPos(iChartParent,0, 0, 0, 0, 0, SWP_NOZORDER|SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE| SWP_FRAMECHANGED);
      }
      if(currChart<0) break;
      prevChart=currChart;
      currChart=ChartNext(prevChart);
      i++;
   }
   return(0);
}
//+------------------------------------------------------------------+