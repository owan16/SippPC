
// HID_UARTDlg.cpp : implementation file
//

#include "stdafx.h"
#include "HID_UART.h"
#include "HID_UARTDlg.h"
#include "afxdialogex.h"

#define WM_COMM_RXCHAR     WM_USER+7       // A character was received and placed in the input buffer.

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


HINSTANCE hLib;
HANDLE hWDevice;
HANDLE hRDevice;
BOOL (*SetFeature)(HANDLE hDevice, LPVOID pData, DWORD nLen);
BOOL (*GetFeature)(HANDLE hDevice, LPVOID pData, DWORD nLen);
void (*CloseHIDDevice)(HANDLE hDevice);
BOOL (*HIDDeviceExist)(DWORD wVID, DWORD wPID, DWORD wUsagePage, DWORD wUsage);
HANDLE (*OpenFirstHIDDevice)(DWORD wVID, DWORD wPID,DWORD wUsagePage, DWORD wUsage, BOOL bSync);
HANDLE (*OpenNextHIDDevice)(DWORD wVID, DWORD wPID,DWORD wUsagePage, DWORD wUsage, BOOL bSync);
//BOOL (*SetInputBuffer)(HANDLE hDevice,ULONG len);

/////////////////////////////////////////////////////////////////////////////
// MakeFileNameFromModulePath

CString MakeFileNameFromModulePath(HINSTANCE hInst,CString strFile)
{
  wchar_t szFile[_MAX_PATH],*pos;
  GetModuleFileName(hInst,szFile,_MAX_PATH);
  pos=wcsrchr(szFile,_T('\\'));
  *(pos+1)=0;
  wcscat(szFile,strFile);
  return szFile;
} 

/////////////////////////////////////////////////////////////////////////////
// GetToken

CString GetToken(CString& tmpStr,char sep)
{
   CString token;
   tmpStr.TrimLeft(); tmpStr.TrimRight();

   int index;
   int len=tmpStr.GetLength();

   if(sep==' ')
   {
      int index1=tmpStr.Find(' ');
      int index2=tmpStr.Find('\t');
      if(index1!=-1 && index2!=-1)
      {
         if(index1<index2) index=index1;
         else index=index2;
      }else if(index1==-1 && index2==-1) index=-1;
      else if(index1==-1) index=index2;
      else index=index1;
   }else index=tmpStr.Find(sep);

   if(index!=-1)
   {
      token=tmpStr.Left(index);
      token.TrimRight();
      if(len-index-1==0) tmpStr.Empty();
	  else tmpStr=tmpStr.Right(len-index-1);
      tmpStr.TrimLeft();
   }else
   {
      token=tmpStr;
      tmpStr.Empty();
   }
   return token;
}


/////////////////////////////////////////////////////////////////////////////
// HTPeekMessage

void HTPeekMessage()
{
   MSG msg;
   if(PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
   {
      TranslateMessage(&msg);
      DispatchMessage(&msg);
   }
}

// CAboutDlg dialog used for App About

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dialog Data
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CHID_UARTDlg dialog




CHID_UARTDlg::CHID_UARTDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CHID_UARTDlg::IDD, pParent)
	, m_nBR(0)
	, m_nBits(0)
	, m_nParity(0)
	, m_strWrData(_T("A B C"))
	, m_nDevice(0)
	, m_Radio4(0)
	, m_nBR8(0)
	, m_nBR9(0)
	, m_nBR11(0)
	, m_nBR12(1)
	, m_nBR13(1)
	, m_nBR14(1)
	, m_nBR15(0)
	, m_nBR16(0)
	, m_nBR17(0)
	, m_nBR18(0)
	, m_strData3(_T("0x01 "))
	, m_strData2(_T("0x12 "))
	, m_strData5(_T("0x0B "))
	, m_strData4(_T("0xB8 "))
	, m_nBR10(0)
	, m_nBR19(0)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CHID_UARTDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_CBIndex(pDX, IDC_BR, m_nBR);
	DDX_CBIndex(pDX, IDC_STOPB, m_nBits);
	DDX_CBIndex(pDX, IDC_PARITY, m_nParity);
	DDX_Text(pDX, IDC_DATA, m_strWrData);
	DDX_Control(pDX, IDC_READ, m_ctlList);
	DDX_Control(pDX, IDC_START, m_ctlStart);
	DDX_Control(pDX, IDC_WRITE, m_ctlWrite);
	DDX_Control(pDX, IDC_DEVICE, m_ctlDevice);
	DDX_CBIndex(pDX, IDC_DEVICE, m_nDevice);

	DDX_Control(pDX, IDC_READ2, m_ctlList2);

	DDX_Radio(pDX, IDC_RADIO11, m_Radio4);
	DDX_CBIndex(pDX, IDC_BR8, m_nBR8);
	DDX_CBIndex(pDX, IDC_BR9, m_nBR9);
	DDX_Text(pDX, IDC_DATA3, m_strData3);
	DDX_Text(pDX, IDC_DATA2, m_strData2);
	DDX_CBIndex(pDX, IDC_BR11, m_nBR11);
	DDX_CBIndex(pDX, IDC_BR12, m_nBR12);
	DDX_CBIndex(pDX, IDC_BR13, m_nBR13);
	DDX_CBIndex(pDX, IDC_BR14, m_nBR14);
	DDX_CBIndex(pDX, IDC_BR15, m_nBR15);
	DDX_CBIndex(pDX, IDC_BR16, m_nBR16);
	DDX_CBIndex(pDX, IDC_BR17, m_nBR17);
	DDX_CBIndex(pDX, IDC_BR18, m_nBR18);
	DDX_Text(pDX, IDC_DATA5, m_strData5);
	DDX_Text(pDX, IDC_DATA4, m_strData4);
	DDX_CBIndex(pDX, IDC_BR10, m_nBR10);
	DDX_CBIndex(pDX, IDC_BR19, m_nBR19);
	
}

BEGIN_MESSAGE_MAP(CHID_UARTDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_START, &CHID_UARTDlg::OnBnClickedStart)
	ON_BN_CLICKED(IDC_OPEN, &CHID_UARTDlg::OnBnClickedOpen)
	ON_BN_CLICKED(IDC_WRITE, &CHID_UARTDlg::OnBnClickedWrite)
	ON_MESSAGE(WM_COMM_RXCHAR,OnCommRxchar)
	ON_BN_CLICKED(IDC_CLRRX, &CHID_UARTDlg::OnBnClickedClrrx)
	ON_BN_CLICKED(IDC_CLRTX, &CHID_UARTDlg::OnBnClickedClrtx)
	ON_BN_CLICKED(IDC_NEW, &CHID_UARTDlg::OnBnClickedNew)
	ON_CBN_SELCHANGE(IDC_BR, &CHID_UARTDlg::OnCbnSelchangeBr)
	ON_CBN_SELCHANGE(IDC_STOPB, &CHID_UARTDlg::OnCbnSelchangeStopb)
	ON_CBN_SELCHANGE(IDC_PARITY, &CHID_UARTDlg::OnCbnSelchangeParity)
	ON_CBN_SELCHANGE(IDC_DEVICE, &CHID_UARTDlg::OnCbnSelchangeDevice)
	ON_WM_CTLCOLOR()
END_MESSAGE_MAP()


// CHID_UARTDlg message handlers

BOOL CHID_UARTDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	CString strLib = MakeFileNameFromModulePath(AfxGetInstanceHandle(),_T("HIDApi.dll"));
	hLib = LoadLibrary(strLib);
    if(!hLib) 
    {
        AfxMessageBox(_T("Failed to load dll!"));
        return FALSE;
    }

	(FARPROC&)OpenFirstHIDDevice=GetProcAddress(hLib,"OpenFirstHIDDevice");
	(FARPROC&)OpenNextHIDDevice=GetProcAddress(hLib,"OpenNextHIDDevice");
    (FARPROC&)CloseHIDDevice=GetProcAddress(hLib,"CloseHIDDevice");
    (FARPROC&)SetFeature=GetProcAddress(hLib,"SetFeature");
    (FARPROC&)GetFeature=GetProcAddress(hLib,"GetFeature");
    (FARPROC&)HIDDeviceExist=GetProcAddress(hLib,"HIDDeviceExist");
	//(FARPROC&)SetInputBuffer=GetProcAddress(hLib,"SetInputBuffer");

	if(!OpenFirstHIDDevice || !OpenNextHIDDevice || !CloseHIDDevice ||
		!SetFeature || !GetFeature || !HIDDeviceExist/* || !SetInputBuffer*/)
	{
		AfxMessageBox(_T("Failed to get DLL function!"));
		return FALSE;
	}

	OnBnClickedNew();
	m_bStart = FALSE;
	m_ctlList.AddString(_T(""));
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CHID_UARTDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CHID_UARTDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CHID_UARTDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


DWORD br[] = {9600,19200,38400,57600,115200};
void CHID_UARTDlg::OnBnClickedStart()
{
	OVERLAPPED  ov;
	HANDLE hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	CString str;
	int n = 2, value;
	DWORD dw;
	char szBuf[9];
	ZeroMemory(szBuf, 9);

	UpdateData();

	if(m_bStart)
	{
		m_ctlStart.SetWindowText(_T("Fuck"));
		m_ctlWrite.EnableWindow(FALSE);
		m_bStart = FALSE;

		//Send Disconnect PC
		str += "0x31 ";
		str += "0x0a ";
		while (!str.IsEmpty())
		{
			CString s = GetToken(str, ' ');
			s.MakeUpper();
			swscanf(s, _T("%x"), &value);
			szBuf[n++] = (BYTE)value;
		}
		szBuf[1] = n - 2;
		memset(&ov, 0, sizeof(OVERLAPPED));
		ov.hEvent = hEvent;
		WriteFile(hWDevice, szBuf, 33, &dw, &ov);
		CloseHandle(ov.hEvent);
		StopThread();
		CloseHIDDevice(hWDevice);
		CloseHIDDevice(hRDevice);


		return;
	}	

	hWDevice=OpenDevice(m_nDevice,FALSE);
	hRDevice=OpenDevice(m_nDevice,TRUE);
	if(hWDevice==NULL || hRDevice==NULL)
	{
		AfxMessageBox(_T("Failed to open Holtek HID_USB Bridge"));
		return;
	}
	//SetInputBuffer(hRDevice,40960);
	
	ZeroMemory(szBuf, 9);

	m_nBR = 4;
	m_nBits = 0;
	m_nParity = 0;

	szBuf[1] = 0x01;	//command code
	szBuf[2] = (BYTE)(br[m_nBR]);
	szBuf[3] = (BYTE)(br[m_nBR] >> 8);
	szBuf[4] = (BYTE)(br[m_nBR] >> 16);
	szBuf[5] = (BYTE)(br[m_nBR] >> 24);
	szBuf[6] = m_nBits;
	szBuf[7] = m_nParity;
	szBuf[8] = 0x08;
	SetFeature(hWDevice, szBuf, 9);
	

	m_bStart = TRUE;
	m_Thread = AfxBeginThread(CommThread, this, THREAD_PRIORITY_NORMAL);
	m_ctlStart.SetWindowText(_T("Stooooooop"));
	m_ctlWrite.EnableWindow(TRUE);

	//Send Connect PC
	ZeroMemory(szBuf, 9);
	str += "0x31 ";
	str += "0x0a ";
	while (!str.IsEmpty())
	{
		CString s = GetToken(str, ' ');
		s.MakeUpper();
		swscanf(s, _T("%x"), &value);
		szBuf[n++] = (BYTE)value;
	}
	szBuf[1] = n - 2;
	memset(&ov, 0, sizeof(OVERLAPPED));
	ov.hEvent = hEvent;
	WriteFile(hWDevice, szBuf, 33, &dw, &ov);
	CloseHandle(ov.hEvent);
}

void CHID_UARTDlg::StopThread()
{
	if(m_Thread != NULL)
	{
		int nIndex=0;
		do
		{
			BOOL b = CancelSynchronousIo(m_Thread->m_hThread);
			HTPeekMessage();
			m_bStart = FALSE;
			Sleep(1);
			nIndex++;
		}while (m_bThreadAlive && nIndex<2000);
		if(nIndex==2000)
		{
			TerminateThread(m_Thread->m_hThread,100);
			delete m_Thread;
		}
		TRACE("Thread ended\n");
   }
}

UINT CHID_UARTDlg::CommThread(LPVOID pParam)
{
	CHID_UARTDlg *pDlg = (CHID_UARTDlg*)pParam;
	
	pDlg->m_bThreadAlive = TRUE;
	while(pDlg->m_bStart)
	{
		unsigned char szBuf[33];
		DWORD dw;

		if(ReadFile(hRDevice,szBuf,33,&dw,NULL))
		{
			pDlg->SendMessage(WM_COMM_RXCHAR,(WPARAM)szBuf,0);
			printf("test");
#if 0
			CString str,s;
			for(int i=2;i<szBuf[1]+2;i++)
			{
				s.Format(_T("%02X "),szBuf[i]);
				str+=s;
			}
			OutputDebugString(str);
#endif
		}
	}

    pDlg->m_bThreadAlive = FALSE;
    // Kill this thread.  break is not needed, but makes me feel better.
    AfxEndThread(100);
#if 0
	OutputDebugString(_T("end\n")); 
#endif
	return 0;
}

LRESULT CHID_UARTDlg::OnCommRxchar(WPARAM wParam,LPARAM lParam)
{
	unsigned char *szBuf = (unsigned char*)wParam;
	CString str,s;

	int n1 = m_ctlList.GetCount();
	n1--;
	int n2 = m_ctlList.GetTextLen(n1);
	m_ctlList.GetText(n1,str);

	for(int i=2;i<szBuf[1]+2;i++)
	{
		s.Format(_T("%02X "),szBuf[i]);
		str+=s;
		n2+=3;
		if(n2 >= 96) //32*3
		{
			m_ctlList.DeleteString(n1);
			n1 = m_ctlList.AddString(str);
			m_ctlList.SetCurSel(n1);
			n1 = m_ctlList.AddString(_T(""));
			n2 = 0;
			str.Empty();
		}
	}
	if(n2)
	{
		m_ctlList.DeleteString(n1);
		n1 = m_ctlList.AddString(str);
		m_ctlList.SetCurSel(n1);
	}
	return 0;
}

void CHID_UARTDlg::OnBnClickedOpen()
{
	UpdateData();
	CFileDialog dlg(TRUE,_T("txt"),_T("*.txt"),0,_T("Text File(*.txt)|*.txt||"),NULL);
   if(dlg.DoModal()==IDOK)
   {
      CString strFile=dlg.GetPathName();

      m_strWrData.Empty();
      UpdateData(FALSE);
      CStdioFile file;
      if(!file.Open(strFile,CFile::modeRead,0))
      {
         AfxMessageBox(_T("Failed to open file!"));
         return;
      }
      DWORD i=0;
      CString str;
      while(file.ReadString(str))
      {
         while(!str.IsEmpty())
         {
            //int n;
            CString s=GetToken(str,' ');
            s.MakeUpper();
            m_strWrData+=s;
            m_strWrData+=" ";
            //if(wcsnicmp(s,"@D",2)==0) m_strWrData+="\r\n";
            //else 
			if(i++%16==15) m_strWrData+="\r\n";
         }
      }
      file.Close();
   }
   UpdateData(FALSE);
}


void CHID_UARTDlg::OnBnClickedWrite()
{
	// TODO: Add your control notification handler code here
	UpdateData();
	m_ctlList.ResetContent();
	m_ctlList.AddString(_T(""));

	int n = 2,value;
	char szBuf[33];
	DWORD dw;

	//CString str = m_strWrData;
	CString str;
	//Set Device Name
	if (m_Radio4 == 0)
	{
		str += m_strWrData;
	}
	//Get firmware ID
	else if (m_Radio4 == 1)
	{
		str = "0x47 ";
		str += "0x0a ";
	}
	//Set Config
	else if (m_Radio4 == 2)
	{
		str = "0x45 ";
		//Byte0
		if (m_nBR8 == 0) { str += "0x00 "; }
		if (m_nBR8 == 1) { str += "0x01 "; }
		if (m_nBR8 == 2) { str += "0x02 "; }
		if (m_nBR8 == 3) { str += "0x03 "; }

		//Byte1
		if (m_nBR9 == 0) { str += "0x00 "; }
		if (m_nBR9 == 1) { str += "0x01 "; }
		if (m_nBR9 == 2) { str += "0x02 "; }
		if (m_nBR9 == 3) { str += "0x03 "; }

		//Byte2
		str += m_strData3;
		str += " ";

		//Byte3
		str += m_strData2;
		str += " ";

		//Byte4
		if (m_nBR11 == 0) { str += "0x01 "; }
		if (m_nBR11 == 1) { str += "0x02 "; }

		//Byte5
		if (m_nBR12 == 0) { str += "0x00 "; }
		if (m_nBR12 == 1) { str += "0x01 "; }

		//Byte6
		if (m_nBR13 == 0) { str += "0x00 "; }
		if (m_nBR13 == 1) { str += "0x01 "; }

		//Byte7
		if (m_nBR14 == 0) { str += "0x00 "; }
		if (m_nBR14 == 1) { str += "0x01 "; }

		//Byte8
		if (m_nBR15 == 0) { str += "0x07 "; }
		if (m_nBR15 == 1) { str += "0x08 "; }
		if (m_nBR15 == 2) { str += "0x00 "; }
		if (m_nBR15 == 3) { str += "0x01 "; }
		if (m_nBR15 == 4) { str += "0x09 "; }
		if (m_nBR15 == 5) { str += "0x02 "; }

		//Byte9
		if (m_nBR16 == 0) { str += "0x00 "; }
		if (m_nBR16 == 1) { str += "0x01 "; }

		//Byte10
		if (m_nBR17 == 0) { str += "0x00 "; }
		if (m_nBR17 == 1) { str += "0x01 "; }
		if (m_nBR17 == 2) { str += "0x02 "; }
		if (m_nBR17 == 3) { str += "0x03 "; }

		//Byte11
		if (m_nBR18 == 0) { str += "0x00 "; }
		if (m_nBR18 == 1) { str += "0x01 "; }

		//Byte12
		str += m_strData5;
		str += " ";

		//Byte13
		str += m_strData4;
		str += " ";
		str += "0x0a ";
	}
	//Blink LED
	else if (m_Radio4 == 3)
	{
		str += "0x37 ";

		//Byte0
		if (m_nBR10 == 0) { str += "0x00 "; }
		if (m_nBR10 == 1) { str += "0x01 "; }

		//Byte1
		if (m_nBR19 == 0) { str += "0x00 "; }
		if (m_nBR19 == 1) { str += "0x01 "; }
		if (m_nBR19 == 2) { str += "0x02 "; }
		str += "0x0a ";
	}
	//Reset Cnt
	else if (m_Radio4 == 4)
	{
		str += "0x36 ";
		str += "0x0a ";
	}
	
	//Read Config
	else if (m_Radio4 == 5)
	{
		str += "0x49 ";
		str += "0x0a ";
	}
	//Connect PC
	else if (m_Radio4 == 6)
	{
		str += "0x31 ";
		str += "0x0a ";
	}
	//Disconnect PC
	else if (m_Radio4 == 7)
	{
		str += "0x31 ";
		str += "0x0a ";
	}
	//Send Data
	else if (m_Radio4 == 8)
	{
		str += "0x32 ";
		str += "0x0a ";
	}
	//Stop Send Data
	else if (m_Radio4 == 9)
	{
		str += "0x33 ";
		str += "0x0a ";
	}
	
	do
	{
		n = str.Replace(_T("\r"), _T(" "));
	}while(n!=0);

	do
	{
		n = str.Replace(_T("\n"), _T(" "));
	}while(n!=0);
	if (m_Radio4 == 0)
	{
		n = 3;
	}
	else
	{
		n = 2;
	}
	ZeroMemory(szBuf,33);

	OVERLAPPED  ov;
	HANDLE hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	
	while(!str.IsEmpty())
    {
		if (m_Radio4 == 0) 
		{
			CString s = GetToken(str, ' ');
			int ASCII = 0;
			ASCII = s.GetAt(0);
			szBuf[n++] = (BYTE)ASCII;
		}
		else 
		{
			CString s = GetToken(str, ' ');
			s.MakeUpper();
			swscanf(s, _T("%x"), &value);
			szBuf[n++] = (BYTE)value;
		}
    }
	if (m_Radio4 == 0)
	{
		CString s;
		s.Format(_T("%x"), 0x42);
		swscanf(s, _T("%x"), &value);
		szBuf[2] = (BYTE)value;
		s.Format(_T("%x"), 0x0a);
		swscanf(s, _T("%x"), &value);
		szBuf[n++] = (BYTE)value;
	
	}
	if(n>2)
	{
		szBuf[1] = n-2;
		memset(&ov, 0, sizeof(OVERLAPPED));    
		ov.hEvent = hEvent;
		WriteFile(hWDevice,szBuf,33,&dw,&ov);
		while(GetOverlappedResult(hWDevice,&ov,&dw,FALSE)==ERROR_IO_INCOMPLETE || dw!=33)
		{
			HTPeekMessage();
		}
	}
	/*
	if (n > 2)
	{
		szBuf[1] = 3;
		szBuf[2] = 0x42;
		szBuf[3] = 0x45;
		memset(&ov, 0, sizeof(OVERLAPPED));
		ov.hEvent = hEvent;
		WriteFile(hWDevice, szBuf, 33, &dw, &ov);
		while (GetOverlappedResult(hWDevice, &ov, &dw, FALSE) == ERROR_IO_INCOMPLETE || dw != 33)
		{
			HTPeekMessage();
		}
	}
	*/
	CloseHandle(ov.hEvent);
}


void CHID_UARTDlg::OnBnClickedClrrx()
{
	m_ctlList.ResetContent();
	m_ctlList.AddString(_T(""));
}


void CHID_UARTDlg::OnBnClickedClrtx()
{
	m_strWrData.Empty();
	UpdateData(FALSE);
}


void CHID_UARTDlg::OnBnClickedNew()
{
	int nCnt = 0;

	m_ctlDevice.ResetContent();

	HANDLE hD = OpenFirstHIDDevice(0x04D9,0xB564,0,0,TRUE);
	if(hD == NULL) return;
	while(hD)
	{
		nCnt++;
		CloseHIDDevice(hD);
		CString s;
		s.Format(_T("%d"),nCnt);
		m_ctlDevice.AddString(s);
		hD = OpenNextHIDDevice(0x04D9,0xB564,0,0,TRUE);
	}
	m_ctlDevice.SetCurSel(0);
}

HANDLE CHID_UARTDlg::OpenDevice(int nIdx,BOOL bSync)
{
	HANDLE hD = OpenFirstHIDDevice(0x04D9,0xB564,0,0,bSync);
	if(nIdx == 0) return hD;	
	for(int i = 0;i < nIdx; i++)
	{
		if(hD) CloseHIDDevice(hD);
		hD = OpenNextHIDDevice(0x04D9,0xB564,0,0,TRUE);
	}
	return hD;
}

void CHID_UARTDlg::OnCbnSelchangeBr()
{
	if(m_bStart)
	{
		OnBnClickedStart();	//stop
		OnBnClickedStart();	//restart;
	}
}


void CHID_UARTDlg::OnCbnSelchangeStopb()
{
	OnCbnSelchangeBr();
}


void CHID_UARTDlg::OnCbnSelchangeParity()
{
	OnCbnSelchangeBr();
}


void CHID_UARTDlg::OnCbnSelchangeDevice()
{
	OnCbnSelchangeBr();
}


HBRUSH CHID_UARTDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	pDC->SetTextColor(RGB(0,0,255));
    pDC->SetBkMode(OPAQUE);
	return hbr;
}
 