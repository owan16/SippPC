
// HID_UARTDlg.h : header file
//

#pragma once
#include "afxwin.h"


// CHID_UARTDlg dialog
class CHID_UARTDlg : public CDialogEx
{
// Construction
public:
	CHID_UARTDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_HID_UART_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	int m_nBR;
	int m_nBits;
	int m_nParity;
	BOOL m_bStart;
	BOOL m_bThreadAlive;
	CString m_strWrData;
	CWinThread*  m_Thread;

	void StopThread();
	HANDLE OpenDevice(int nIdx,BOOL bSync);
	afx_msg void OnBnClickedStart();
	afx_msg void OnBnClickedOpen();
	afx_msg void OnBnClickedWrite();
	afx_msg LRESULT OnCommRxchar(WPARAM rxchar,LPARAM nPortNr);

	static UINT  CommThread(LPVOID pParam);
	CListBox m_ctlList;

	CListBox m_ctlList2;

	int m_Radio4;
	int m_nBR8;
	int m_nBR9;
	CString m_strData3;
	CString m_strData2;
	int m_nBR11;
	int m_nBR12;
	int m_nBR13;
	int m_nBR14;
	int m_nBR15;
	int m_nBR16;
	int m_nBR17;
	int m_nBR18;
	CString m_strData5;
	CString m_strData4;
	int m_nBR10;
	int m_nBR19;

	CButton m_ctlStart;
	afx_msg void OnBnClickedClrrx();
	afx_msg void OnBnClickedClrtx();
	CButton m_ctlWrite;
	afx_msg void OnBnClickedNew();
	CComboBox m_ctlDevice;
	int m_nDevice;
	afx_msg void OnCbnSelchangeBr();
	afx_msg void OnCbnSelchangeStopb();
	afx_msg void OnCbnSelchangeParity();
	afx_msg void OnCbnSelchangeDevice();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
};
