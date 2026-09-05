import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ErpService } from '../../services/erp.service';

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.css']
})
export class AiAssistantComponent {
  currentInput = '';
  messages = [
    { sender: 'ai', text: 'Hello! I am your ERP AI Copilot. How can I assist you with inventory levels, supplier management, or purchase order workflows today?' }
  ];

  constructor(private erpService: ErpService, private cdr: ChangeDetectorRef) {}

  sendMessage() {
    if (!this.currentInput.trim()) return;
    const text = this.currentInput;
    this.messages.push({ sender: 'user', text });
    this.currentInput = '';
    this.cdr.detectChanges();

    this.erpService.askAi(text).subscribe({
      next: (res: any) => {
        const aiResponse = res?.data?.response || 'I have successfully processed your request.';
        const toolUsed = res?.data?.toolCalled ? ` (Tool executed: \`${res.data.toolCalled}\`)` : '';
        this.messages.push({ sender: 'ai', text: aiResponse + toolUsed });
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('AI Agent error:', err);
        this.messages.push({ sender: 'ai', text: 'Sorry, I encountered an error communicating with the AI Agent service. Ensure AIAgentService is running.' });
        this.cdr.detectChanges();
      }
    });
  }

  sendPreset(prompt: string) {
    this.currentInput = prompt;
    this.sendMessage();
  }
}
