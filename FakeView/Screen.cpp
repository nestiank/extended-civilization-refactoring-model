﻿#include "pch.h"
#include "Screen.h"

namespace FakeView
{
    Screen::Screen()
    {
        ::InitializeCriticalSection(&m_crit);
        ClearBuffer();
    }

    int Screen::Loop(IScreenClient^ view)
    {
        std::system("mode con cols=150 lines=45");
        std::system("cls");
        EnableCursor(false);

        DWORD prevTick = GetTickCount();

        while (!m_quit)
        {
            ClearBuffer();
            view->Render();
            DrawBuffer();

            while (!_kbhit() && m_invokee.empty())
            {
                DWORD nowTick = GetTickCount();
                if (nowTick - prevTick >= 100)
                {
                    prevTick += 100;

                    view->OnTick();

                    ClearBuffer();
                    view->Render();
                    DrawBuffer();
                }
                else
                {
                    ::Sleep(1);
                }
            }

            if (_kbhit())
            {
                int ch = _getch();
                // It seems that getch() returns 0 on a synchronization issue
                if (/*ch == 0 || */ch == 0xe0)
                {
                    ch = _getch() | 0x0100;
                }

                view->OnKeyStroke(ch);
            }
            else if (!m_invokee.empty())
            {
                try
                {
                    ::EnterCriticalSection(&m_crit);
                    System::Action^ act = m_invokee.front();
                    m_invokee.pop_front();
                    act();
                }
                finally
                {
                    ::LeaveCriticalSection(&m_crit);
                }
            }
        }

        return m_exitcode;
    }

    void Screen::Invoke(System::Action^ act)
    {
        try
        {
            ::EnterCriticalSection(&m_crit);
            m_invokee.push_back(act);
        }
        finally
        {
            ::LeaveCriticalSection(&m_crit);
        }
    }

    void Screen::ClearBuffer()
    {
        for (auto& c : m_buffer)
        {
            c.ch = ' ';
            c.color = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
        }
    }

    void Screen::DrawBuffer()
    {
        GotoXY(0, 0);
        for (const auto& c : m_buffer)
        {
            ::SetConsoleTextAttribute(::GetStdHandle(STD_OUTPUT_HANDLE), c.color);
            _putch(c.ch);
        }
    }

    void Screen::Quit(int exitcode)
    {
        m_exitcode = exitcode;
        m_quit = true;
    }

    Character& Screen::GetChar(int x, int y)
    {
        auto sz = GetSize();
        return m_buffer.at(y * sz.width + x);
    }

    Character* Screen::TryGetChar(int x, int y)
    {
        auto sz = GetSize();
        int idx = y * sz.width + x;
        if (static_cast<std::size_t>(idx) >= m_buffer.size())
            return nullptr;

        return &m_buffer[idx];
    }

    void Screen::GotoXY(int x, int y)
    {
        COORD Cur = { static_cast<SHORT>(x), static_cast<SHORT>(y) };
        ::SetConsoleCursorPosition(::GetStdHandle(STD_OUTPUT_HANDLE), Cur);
    }

    void Screen::EnableCursor(bool enable)
    {
        CONSOLE_CURSOR_INFO CurInfo;
        if (enable)
        {
            CurInfo.dwSize = 20;
            CurInfo.bVisible = TRUE;
        }
        else
        {
            CurInfo.dwSize = 1;
            CurInfo.bVisible = FALSE;
        }
        ::SetConsoleCursorInfo(::GetStdHandle(STD_OUTPUT_HANDLE), &CurInfo);
    }

    Size Screen::GetSize()
    {
        /*CONSOLE_SCREEN_BUFFER_INFO csbi;
        ::GetConsoleScreenBufferInfo(::GetStdHandle(STD_OUTPUT_HANDLE), &csbi);
        return {
            csbi.srWindow.Right - csbi.srWindow.Left + 1,
            csbi.srWindow.Bottom - csbi.srWindow.Top + 1
        };*/
        return { 150, 45 };
    }

    void Screen::PrintString(int x, int y, unsigned char color, const std::string& str)
    {
        if (x < 0 || y < 0)
            return;
        auto scrsz = GetSize();
        std::size_t bufidx = x + y * scrsz.width;
        for (std::size_t idx = 0; idx < str.size(); ++idx)
        {
            if (bufidx >= m_buffer.size())
                break;

            m_buffer[bufidx].ch = (str[idx] & 0x80) ? '?' : str[idx];
            m_buffer[bufidx].color = color;
            ++bufidx;
        }
    }

    void Screen::PrintStringEx(int x, int y, unsigned char color, const std::string& str)
    {
        if (x < 0 || y < 0)
            return;
        auto scrsz = GetSize();
        std::size_t bufidx = x + y * scrsz.width;
        for (std::size_t idx = 0; idx < str.size(); ++idx)
        {
            if (bufidx >= m_buffer.size())
                break;

            if (str[idx] != '%')
            {
            print:
                m_buffer[bufidx].ch = (str[idx] & 0x80) ? '?' : str[idx];
                m_buffer[bufidx].color = color;
                ++bufidx;
            }
            else
            {
                if (idx + 2 >= str.size())
                    goto print;
                if (str[++idx] != 'c')
                    goto print;

                color = static_cast<unsigned char>(str[++idx]);
            }
        }
    }
}
