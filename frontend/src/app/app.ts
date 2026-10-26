import { Component, inject, signal, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private http = inject(HttpClient);

  healthStatus = signal<string>('Checking backend...');

  ngOnInit(): void {
    this.http.get('/api/health', { responseType: 'text' }).subscribe({
      next: (status) => this.healthStatus.set(`Backend OK: ${status}`),
      error: (err) => this.healthStatus.set(`Backend failed: ${err.status} ${err.statusText}`),
    });
  }
}
